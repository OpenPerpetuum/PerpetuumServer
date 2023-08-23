using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public class TechTreeNode : IEquatable<TechTreeNode>
    {
        private readonly int _definition;
        private readonly int _parentDefinition;
        private readonly TechTreeGroup _group;
        private readonly int _x;
        private readonly int _y;

        public int X => _x;
        public int Definition => _definition;
        public int ParentDefinition => _parentDefinition;
        public TechTreeGroup Group => _group;

        public IEnumerable<Points> Prices { get; set; }

        private TechTreeNode(int definition, int parentDefinition, TechTreeGroup group, int x, int y)
        {
            _definition = definition;
            _parentDefinition = parentDefinition;
            _group = group;
            _x = x;
            _y = y;
        }

        public static TechTreeNode CreateFromDataRecord(IDataRecord record)
        {
            var definition = record.GetValue<int>("childdefinition");
            var parentDefinition = record.GetValue<int>("parentdefinition");
            var group = record.GetValue<TechTreeGroup>("groupId");
            var x = record.GetValue<int>("x");
            var y = record.GetValue<int>("y");
            return new TechTreeNode(definition, parentDefinition, group, x, y);
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {"definition", Definition},
                    {"parentdefinition", ParentDefinition},
                    {"groupId", (int)Group},
                    {"x", X},
                    {"y", _y},
                    {"prices",Prices.ToDictionary("p",p => p.ToDictionary())}
                };
        }

        public override string ToString()
        {
            return $"Definition: {Definition}, ParentDefinition: {ParentDefinition}, GroupId: {Group}, X: {X}, Y: {_y}";
        }

        public List<TechTreeNode> Traverse(IDictionary<int, TechTreeNode> nodes)
        {
            var r = new List<TechTreeNode>();

            var parentDefinition = ParentDefinition;

            while (parentDefinition > 0)
            {
                TechTreeNode parentNode;
                if (!nodes.TryGetValue(parentDefinition, out parentNode))
                    break;

                r.Add(parentNode);
                parentDefinition = parentNode.ParentDefinition;
            }

            return r;
        }

        public Extension GetEnablerExtension(IDictionary<TechTreeGroup, TechTreeGroupInfo> groups)
        {
            var enablerExtensionId = groups[Group].enablerExtensionId;
            return new Extension(enablerExtensionId, X + 1);
        }

        public bool Equals(TechTreeNode other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return _y == other._y && Definition == other.Definition && ParentDefinition == other.ParentDefinition && Group == other.Group && X == other.X;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((TechTreeNode)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _definition;
                hashCode = (hashCode * 397) ^ _parentDefinition;
                hashCode = (hashCode * 397) ^ (int)_group;
                hashCode = (hashCode * 397) ^ _x;
                hashCode = (hashCode * 397) ^ _y;
                return hashCode;
            }
        }

        public static bool operator ==(TechTreeNode left, TechTreeNode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TechTreeNode left, TechTreeNode right)
        {
            return !Equals(left, right);
        }
    }
}