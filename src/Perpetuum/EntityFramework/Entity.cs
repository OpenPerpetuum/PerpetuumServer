using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Transactions;

namespace Perpetuum.EntityFramework
{
    public class Entity : IEntity
    {
        public static IEntityServices Services { get; set; }

        public static IEntityFactory Factory => Services.Factory;
        public static IEntityRepository Repository => Services.Repository;

        public IEntityServices EntityServices { protected get; set; }

        private ImmutableHashSet<Entity> _children = ImmutableHashSet<Entity>.Empty;
        internal EntityDbState dbState = EntityDbState.New;
        private double _health;
        private string _name;
        private long _owner;
        private long _parent;
        private int _quantity;
        private bool _repackaged;

        public Entity()
        {
            DynamicProperties = new EntityDynamicProperties();
            DynamicProperties.Updated += OnDynamicPropertiesUpdated;
        }

        public EntityDynamicProperties DynamicProperties { get; }

        public long Eid { get; set; }
        public EntityDefault ED { get; set; }

        public int Definition => ED.Definition;

        public long Owner
        {
            get => _owner;
            set
            {
                if (_owner != value)
                {
                    _owner = value;
                    OnPropertyChanged();
                }

                foreach (Entity child in _children)
                {
                    child.Owner = value;
                }
            }
        }

        public long Parent
        {
            get => _parent;
            set
            {
                if (_parent == value)
                {
                    return;
                }

                _parent = value;
                OnPropertyChanged();
            }
        }

        [CanBeNull]
        public Entity ParentEntity { get; set; }

        [CanBeNull]
        public Entity GetOrLoadParentEntity()
        {
            if ((ParentEntity == null || ParentEntity.Eid <= 0) && Parent > 0)
            {
                ParentEntity = LoadParentEntity(Parent);
            }

            return ParentEntity;
        }

        protected virtual Entity LoadParentEntity(long parent)
        {
            return Repository.LoadOrThrow(parent);
        }

        public virtual double Health
        {
            get => _health;
            set
            {
                if (Equals(_health, value))
                {
                    return;
                }

                _health = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                {
                    return;
                }

                _name = value;
                OnPropertyChanged();
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value)
                {
                    return;
                }

                _quantity = value;
                OnPropertyChanged();
            }
        }

        public bool IsRepackaged
        {
            get => _repackaged;
            set
            {
                if (_repackaged == value)
                {
                    return;
                }

                _repackaged = value;
                OnPropertyChanged();
            }
        }

        public virtual double Volume => ED.CalculateVolume(IsRepackaged, Quantity);

        public virtual double Mass => ED.Mass;

        public IReadOnlyCollection<Entity> Children => _children;

        public bool HasChildren => _children.Count > 0;

        public double HealthRatio => (Health / ED.Health).Clamp();

        protected static bool TryAcceptVisitor<T>(T entity, IEntityVisitor visitor) where T : Entity
        {
            if (!(visitor is IEntityVisitor<T> v))
            {
                return false;
            }

            v.Visit(entity);
            return true;
        }

        public virtual void AcceptVisitor(IEntityVisitor visitor)
        {
            TryAcceptVisitor(this, visitor);
        }

        private void OnDynamicPropertiesUpdated()
        {
            OnPropertyChanged();
        }

        private void OnPropertyChanged()
        {
            if (dbState == EntityDbState.New)
            {
                return;
            }

            dbState = EntityDbState.Updated;
        }

        public virtual void OnLoadFromDb() { }
        public virtual void OnSaveToDb() { }
        public virtual void OnInsertToDb() { }
        public virtual void OnUpdateToDb() { }
        public virtual void OnDeleteFromDb() { }

        public List<Entity> GetFullTree()
        {
            List<Entity> entities = new List<Entity>();

            foreach (Entity child in Children)
            {
                entities.AddRange(child.GetFullTree());
            }

            entities.Add(this);
            return entities;
        }

        public virtual Dictionary<string, object> ToDictionary()
        {
            return BaseInfoToDictionary();
        }

        public Dictionary<string, object> BaseInfoToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.eid, Eid},
                {k.definition, Definition},
                {k.parent, Parent},
                {k.owner, Owner},
                {k.health, Health},
                {k.repackaged, IsRepackaged},
                {k.quantity, Quantity},
                {k.name, Name},
                {k.volume, Volume},
            };
        }

        public override string ToString()
        {
            return string.Format("eid:{0} {2} ({1}) o:{3} p:{4} h:{5} n:{6} q:{7} r:{8}", Eid, Definition, ED.Name, Owner, Parent, Health,
                                 Name, Quantity, IsRepackaged);
        }

        public bool IsCategory(CategoryFlags targetCategoryFlags)
        {
            return ED.CategoryFlags.IsCategory(targetCategoryFlags);
        }

        public void AddChild(Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            entity.ParentEntity?.RemoveChild(entity);

            ImmutableInterlocked.Update(ref _children, c => c.Add(entity));

            entity.Parent = Eid;
            entity.ParentEntity = this;
        }


        public void RemoveChild(Entity entity)
        {
            ImmutableInterlocked.Update(ref _children, c => c.Remove(entity));

            entity.Parent = 0;
            entity.ParentEntity = null;
        }

        protected void ClearChildren()
        {
            ImmutableInterlocked.Update(ref _children, c => c.Clear());
        }

        protected internal void RebuildTree(IEnumerable<Entity> entities)
        {
            Dictionary<long, Entity> x = entities.ToDictionary(e => e.Eid);

            foreach (IGrouping<long, Entity> child in x.Values.GroupBy(kvp => kvp.Parent))
            {
                Entity parentEntity = child.Key == Eid ? this : x.GetOrDefault(child.Key);
                parentEntity?.AddManyChild(child);
            }
        }

        private void AddManyChild(IEnumerable<Entity> children)
        {
            ImmutableInterlocked.Update(ref _children, c =>
            {
                ImmutableHashSet<Entity>.Builder b = ImmutableHashSet<Entity>.Empty.ToBuilder();
                foreach (Entity child in children)
                {
                    b.Add(child);
                    child.Parent = Eid;
                    child.ParentEntity = this;
                    child.OnLoadFromDb();
                }
                return b.ToImmutable();
            });
        }

        [ThreadStatic]
        private static HashSet<Entity> _txEntities;
        private readonly AutoResetEvent _txSync = new AutoResetEvent(true);

        public void EnlistTransaction()
        {
            Transaction currentTx = Transaction.Current;
            if (currentTx == null)
            {
                return;
            }

            if (_txEntities == null)
            {
                _txEntities = new HashSet<Entity>();
            }
            else
            {
                if (_txEntities.Contains(this))
                {
                    return;
                }
            }

            _txEntities.Add(this);
            _txSync.WaitOne(10000);

            // na ez itt a trukk, lokal mentjuk el...
            long owner = _owner;
            long parent = _parent;
            double health = _health;
            string name = _name;
            int quantity = _quantity;
            bool repackaged = _repackaged;
            Entity parentEntity = ParentEntity;
            EntityDbState dbState = this.dbState;
            ImmutableHashSet<Entity> children = _children;
            ImmutableDictionary<string, object> dynProps = DynamicProperties.Items;

            OnEnlistTransaction();

            HashSet<Entity> txEntities = _txEntities;

            currentTx.EnlistVolatile(onCommit: OnCommitedTransaction,
                                     onRollback: () =>
                                     {
                                         try
                                         {
                                             OnRollbackTransaction();
                                         }
                                         finally
                                         {
                                             _owner = owner;
                                             _parent = parent;
                                             _health = health;
                                             _name = name;
                                             _quantity = quantity;
                                             _repackaged = repackaged;
                                             ParentEntity = parentEntity;
                                             this.dbState = dbState;
                                             _children = children;
                                             DynamicProperties.Items = dynProps;
                                         }
                                     },
                                    onCompleted: () =>
                                    {
                                        try
                                        {
                                            OnCompletedTransaction();
                                        }
                                        finally
                                        {
                                            txEntities.Remove(this);
                                            _txSync.Set();
                                        }
                                    });

            foreach (Entity child in Children)
            {
                child.EnlistTransaction();
            }
        }

        protected virtual void OnEnlistTransaction() { }
        protected virtual void OnCommitedTransaction() { }
        protected virtual void OnRollbackTransaction() { }
        protected virtual void OnCompletedTransaction() { }

        public void SetMaxHealth()
        {
            Health = ED.Health;
        }

        public void Save()
        {
            OnSaveToDb();

            foreach (Entity child in Children)
            {
                child.Save();
            }

            switch (dbState)
            {
                case EntityDbState.New:
                    {
                        EntityServices.Repository.Insert(this);
                        break;
                    }
                case EntityDbState.Updated:
                    {
                        EntityServices.Repository.Update(this);
                        break;
                    }
            }
        }
    }
}