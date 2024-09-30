using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Groups.Alliances;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Gangs;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.Social;
using Perpetuum.Services.TechTree;
using Perpetuum.Units.DockingBases;
using Perpetuum.Wallets;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Transactions;

namespace Perpetuum.Accounting.Characters
{
    public delegate Character CharacterFactory(int id);

    public class Character : IEquatable<Character>, IComparable<Character>
    {
        public Character()
        {

        }

        public Character(
            int id,
            IAccountManager accountManager,
            Lazy<IZoneManager> zoneManager,
            DockingBaseHelper dockingBaseHelper,
            RobotHelper robotHelper,
            ICharacterTransactionLogger transactionLogger,
            ICharacterExtensions characterExtensions,
            IExtensionReader extensionReader,
            ISocialService socialService,
            ICorporationManager corporationManager,
            ITechTreeService techTreeService,
            IGangManager gangManager,
            CharacterWalletHelper walletHelper)
        {
            this.accountManager = accountManager;
            this.zoneManager = zoneManager;
            this.dockingBaseHelper = dockingBaseHelper;
            this.robotHelper = robotHelper;
            this.transactionLogger = transactionLogger;
            this.characterExtensions = characterExtensions;
            this.extensionReader = extensionReader;
            this.socialService = socialService;
            this.corporationManager = corporationManager;
            this.techTreeService = techTreeService;
            this.gangManager = gangManager;
            this.walletHelper = walletHelper;

            if (id <= 0)
            {
                id = 0;
            }

            Id = id;
        }

        private static Character none;

        public static Character None => none ?? (none = CharacterFactory(0));

        public static bool IsSystemCharacter(Character c)
        {
            return c.Nick.Contains("[OPP]");  //TODO better configuration of system characters to avoid flimsy name rule
        }

        private readonly IAccountManager accountManager;
        private readonly Lazy<IZoneManager> zoneManager;
        private readonly DockingBaseHelper dockingBaseHelper;
        private readonly RobotHelper robotHelper;
        private readonly ICharacterTransactionLogger transactionLogger;
        private readonly ICharacterExtensions characterExtensions;
        private readonly IExtensionReader extensionReader;
        private readonly ISocialService socialService;
        private readonly ICorporationManager corporationManager;
        private readonly ITechTreeService techTreeService;
        private readonly IGangManager gangManager;
        private readonly CharacterWalletHelper walletHelper;



        public static ObjectCache CharacterCache { get; set; }

        public static CharacterFactory CharacterFactory { get; set; }

        public int Id { get; }

        public long Eid => GetEid(Id);

        public bool IsDocked
        {
            get => ReadValueFromDb<bool>(CharacterConstants.FIELD_DOCKED);
            set => WriteValueToDb(CharacterConstants.FIELD_DOCKED, value);
        }

        public int AccountId
        {
            get => GetCachedAccountId(Id);
            set
            {
                WriteValueToDb(CharacterConstants.FIELD_ACCOUNT_ID, value);
                RemoveAccountIdFromCache();
            }
        }

        public Account GetAccount()
        {
            return accountManager.Repository.Get(AccountId);
        }

        public string Nick
        {
            get => ReadValueFromDb<string>(CharacterConstants.FIELD_NICK);
            set => WriteValueToDb(CharacterConstants.FIELD_NICK, value);
        }

        public bool IsOffensiveNick
        {
            get => ReadValueFromDb<bool>(CharacterConstants.FIELD_OFFENSIVE_NICK);
            set
            {
                WriteValueToDb(CharacterConstants.FIELD_OFFENSIVE_NICK, value);
                WriteValueToDb(CharacterConstants.FIELD_NICK_CORRECTED, !value);
            }
        }

        public double Credit
        {
            get => ReadValueFromDb<double>(CharacterConstants.FIELD_CREDIT);
            set => WriteValueToDb(CharacterConstants.FIELD_CREDIT, value);
        }

        public int MajorId
        {
            get => ReadValueFromDb<int>(CharacterConstants.FIELD_MAJOR_ID);
            set => WriteValueToDb(CharacterConstants.FIELD_MAJOR_ID, value);
        }

        public int RaceId
        {
            get => ReadValueFromDb<int>(CharacterConstants.FIELD_RACE_ID);
            set => WriteValueToDb(CharacterConstants.FIELD_RACE_ID, value);
        }

        public int SchoolId
        {
            get => ReadValueFromDb<int>(CharacterConstants.FIELD_SCHOOL_ID);
            set => WriteValueToDb(CharacterConstants.FIELD_SCHOOL_ID, value);
        }

        public int SparkId
        {
            get => ReadValueFromDb<int>(CharacterConstants.FIELD_SPARK_ID);
            set => WriteValueToDb(CharacterConstants.FIELD_SPARK_ID, value);
        }

        public int Language
        {
            get => ReadValueFromDb<int>(CharacterConstants.FIELD_LANGUAGE);
            set => WriteValueToDb(CharacterConstants.FIELD_LANGUAGE, value);
        }

        public bool IsActive
        {
            get => ReadValueFromDb<bool>(CharacterConstants.FIELD_ACTIVE);
            set
            {
                WriteValueToDb(CharacterConstants.FIELD_ACTIVE, value);

                if (!value)
                {
                    DeletedAt = DateTime.Now;
                }
            }
        }

        public DateTime DeletedAt
        {
            get => ReadValueFromDb<DateTime>(CharacterConstants.FIELD_DELETED_AT);
            set => WriteValueToDb(CharacterConstants.FIELD_DELETED_AT, value);
        }

        public bool IsOnline
        {
            get => ReadValueFromDb<bool>(CharacterConstants.FIELD_IN_USE);
            set => WriteValueToDb(CharacterConstants.FIELD_IN_USE, value);
        }

        public DateTime LastLogout
        {
            get => ReadValueFromDb<DateTime>(CharacterConstants.FIELD_LAST_LOGOUT);
            set => WriteValueToDb(CharacterConstants.FIELD_LAST_LOGOUT, value);
        }

        public DateTime LastRespec
        {
            get => ReadValueFromDb<DateTime>(CharacterConstants.LAST_RESPEC);
            set => WriteValueToDb(CharacterConstants.LAST_RESPEC, value);
        }

        public DateTime LastUsed
        {
            set => WriteValueToDb(CharacterConstants.FIELD_LAST_USED, value);
        }

        public TimeSpan TotalOnlineTime
        {
            get => TimeSpan.FromMinutes(ReadValueFromDb<int>(CharacterConstants.FIELD_TOTAL_MINS_ONLINE));
            set => WriteValueToDb(CharacterConstants.FIELD_TOTAL_MINS_ONLINE, (int)value.TotalMinutes);
        }

        public GenxyString Avatar
        {
            set => WriteValueToDb(CharacterConstants.FIELD_AVATAR, value.ToString());
        }

        public string MoodMessage
        {
            get => ReadValueFromDb<string>(CharacterConstants.FIELD_MOOD_MESSAGE);
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Length > 2000)
                {
                    value = value.Substring(0, 1999);
                }

                WriteValueToDb(CharacterConstants.FIELD_MOOD_MESSAGE, value);
            }
        }

        public int? ZoneId
        {
            get => ReadValueFromDb<int?>(CharacterConstants.FIELD_ZONE_ID);
            set => WriteValueToDb(CharacterConstants.FIELD_ZONE_ID, value);
        }

        public Position? ZonePosition
        {
            get
            {
                double? x = ReadValueFromDb<double?>(CharacterConstants.FIELD_POSITION_X);
                double? y = ReadValueFromDb<double?>(CharacterConstants.FIELD_POSITION_Y);

                return x == null || y == null
                    ? null
                    : (Position?)new Position((double)x, (double)y);
            }
            set
            {
                double? x = null;
                double? y = null;

                if (value != null)
                {
                    Position p = (Position)value;
                    x = p.X;
                    y = p.Y;
                }

                WriteValueToDb(CharacterConstants.FIELD_POSITION_X, x);
                WriteValueToDb(CharacterConstants.FIELD_POSITION_Y, y);
            }
        }

        public long CorporationEid
        {
            get => ReadValueFromDb<long>(CharacterConstants.FIELD_CORPORATION_EID);
            set
            {
                WriteValueToDb(CharacterConstants.FIELD_CORPORATION_EID, value);

                _ = Db.Query().CommandText("update entities set parent=@corporationEID where eid=@characterEID")
                    .SetParameter("@corporationEID", value)
                    .SetParameter("@characterEID", Eid)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
            }
        }

        public long DefaultCorporationEid
        {
            get => ReadValueFromDb<long>(CharacterConstants.FIELD_DEFAULT_CORPORATION_EID);
            set => WriteValueToDb(CharacterConstants.FIELD_DEFAULT_CORPORATION_EID, value);
        }

        public long AllianceEid
        {
            get => ReadValueFromDb<long?>(CharacterConstants.FIELD_ALLIANCE_EID) ?? 0L;
            set => WriteValueToDb(CharacterConstants.FIELD_ALLIANCE_EID, value == 0L ? (object)null : value);
        }

        public long ActiveRobotEid
        {
            get => ReadValueFromDb<long?>(CharacterConstants.FIELD_ACTIVE_CHASSIS) ?? 0;
            set => WriteValueToDb(CharacterConstants.FIELD_ACTIVE_CHASSIS, value == 0L ? (object)null : value);
        }

        public long CurrentDockingBaseEid
        {
            get => ReadValueFromDb<long>(CharacterConstants.FIELD_BASE_EID);
            set => WriteValueToDb(CharacterConstants.FIELD_BASE_EID, value);
        }

        public DateTime NextAvailableUndockTime
        {
            get => GetValueFromCache<DateTime>("nextavailableundocktime");
            set => SetValueToCache("nextavailableundocktime", value);
        }

        public DateTime NextAvailableRobotRequestTime
        {
            get => GetValueFromCache<DateTime>("nextavailablerobotrequesttime");
            set => SetValueToCache("nextavailablerobotrequesttime", value);
        }

        public bool BlockTrades
        {
            get => ReadValueFromDb<bool>(CharacterConstants.FIELD_BLOCK_TRADES);
            set => WriteValueToDb(CharacterConstants.FIELD_BLOCK_TRADES, value);
        }

        public bool GlobalMuted
        {
            get => ReadValueFromDb<bool>(CharacterConstants.FIELD_GLOBAL_MUTE);
            set => WriteValueToDb(CharacterConstants.FIELD_GLOBAL_MUTE, value);
        }

        public AccessLevel AccessLevel => accountManager.Repository.GetAccessLevel(GetCachedAccountId(Id));

        public long? HomeBaseEid
        {
            get => ReadValueFromDb<long?>(CharacterConstants.FIELD_HOME_BASE_EID);
            set => WriteValueToDb(CharacterConstants.FIELD_HOME_BASE_EID, value);
        }

        private static string GetCacheKey(string prefix, object key)
        {
            return $"{prefix}_{key}";
        }

        public void RemoveFromCache()
        {
            _ = CharacterCache.Remove(GetCacheKey(CharacterConstants.CACHE_KEY_ID_TO_EID, Id));
            _ = CharacterCache.Remove(GetCacheKey(CharacterConstants.CACHE_KEY_EID_TO_ID, Eid));
            RemoveAccountIdFromCache();
        }

        public void UpdateExtensionsCache(CharacterExtensionCollection extensions)
        {
            CharacterCache.Set(Id.ToString(), extensions, new TimeSpan(0, 1, 0));
        }

        private void RemoveAccountIdFromCache()
        {
            _ = CharacterCache.Remove(GetCacheKey(CharacterConstants.CACHE_KEY_ID_TO_ACCOUNTID, Id));
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(Character other)
        {
            return Id.CompareTo(other?.Id);
        }

        public override bool Equals(object obj)
        {
            Character other = obj as Character;
            return other != Character.None && Equals(other);
        }

        public bool Equals(Character other)
        {
            return !(other is null) && (ReferenceEquals(this, other) || Id == other.Id);
        }

        public static bool operator ==(Character left, Character right)
        {
            return ReferenceEquals(left, right) || (left is object && right is object && left.Equals(right));
        }

        public static bool operator !=(Character left, Character right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Id}";
        }

        private string CreateCacheKey(string key)
        {
            return $"character_{key}_{Id}";
        }

        private T GetValueFromCache<T>(string key)
        {
            return CharacterCache.Get(CreateCacheKey(key), () => default(T));
        }

        private void SetValueToCache<T>(string key, T value)
        {
            CharacterCache.Set(CreateCacheKey(key), value);
        }

        private T ReadValueFromDb<T>(string name)
        {
            return ReadValueFromDb<T>(Id, name);
        }

        private void WriteValueToDb(string name, object value)
        {
            WriteValueToDb(Id, name, value);
        }

        #region Helpers

        private static T ReadValueFromDb<T>(int id, string name)
        {
            return id == 0
                ? default
                : Db.Query().CommandText("select " + name + " from characters where characterid = @id").SetParameter("@id", id).ExecuteScalar<T>();
        }

        private static T ReadValueFromDb<T>(long eid, string name)
        {
            return eid == 0
                ? default
                : Db.Query().CommandText("select " + name + " from characters where rooteid = @eid").SetParameter("@eid", eid).ExecuteScalar<T>();
        }

        private static void WriteValueToDb(int id, string name, object value)
        {
            if (id == 0)
            {
                return;
            }

            _ = Db.Query().CommandText("update characters set " + name + " = @value where characterid = @id")
                .SetParameter("@id", id)
                .SetParameter("@value", value)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        [NotNull]
        public static Character GetByEid(long characterEid)
        {
            if (characterEid == 0L)
            {
                return None;
            }

            int characterId = GetIdByEid(characterEid);
            return CharacterFactory(characterId);
        }

        [NotNull]
        public static Character Get(int id)
        {
            return CharacterFactory(id);
        }

        private static int GetCachedAccountId(int id)
        {
            return id == 0
                ? 0
                : CharacterCache.Get(
                    GetCacheKey(CharacterConstants.CACHE_KEY_ID_TO_ACCOUNTID, id),
                    () => ReadValueFromDb<int>(id, CharacterConstants.FIELD_ACCOUNT_ID));
        }

        private static long GetEid(int id)
        {
            return id == 0
                ? 0L
                : CharacterCache.Get(
                    GetCacheKey(CharacterConstants.CACHE_KEY_ID_TO_EID, id),
                    () => ReadValueFromDb<long>(id, CharacterConstants.FIELD_ROOT_EID));
        }

        public static int GetIdByEid(long eid)
        {
            return CharacterCache.Get(
                GetCacheKey(CharacterConstants.CACHE_KEY_EID_TO_ID, eid),
                () => ReadValueFromDb<int>(eid, CharacterConstants.FIELD_CHARACTER_ID));
        }

        public static bool Exists(int id)
        {
            return id != 0 && ReadValueFromDb<int>(id, CharacterConstants.FIELD_CHARACTER_ID) > 0;
        }

        public static void CheckNickAndThrowIfFailed(string nick, AccessLevel accessLevel, Account issuerAccount)
        {
            nick = nick.Trim();
            _ = nick.Length.ThrowIfLess(3, ErrorCodes.NickTooShort);
            _ = nick.Length.ThrowIfGreater(25, ErrorCodes.NickTooLong);
            nick.AllowAscii().ThrowIfFalse(ErrorCodes.OnlyAsciiAllowed);
            if (!accessLevel.IsAdminOrGm())
            {
                nick.IsNickAllowedForPlayers().ThrowIfFalse(ErrorCodes.NickReservedForDevelopersAndGameMasters);
            }

            //check history 
            int inHistory =
            Db.Query()
                .CommandText("select count(*) from characternickhistory where nick=@nick and accountid != @accountID")
                .SetParameter("@accountID", issuerAccount.Id)
                .SetParameter("@nick", nick)
                .ExecuteScalar<int>();
            (inHistory > 0).ThrowIfTrue(ErrorCodes.NickTaken);

            //is nick belongs to an active of inavtive character
            Character owner = GetByNick(nick);
            if (owner == Character.None)
            {
                return;
            }

            // ok, now we know that the nick is used in the characters table, lets check ownership and timeouts!

            // an active character has this nick
            owner.IsActive.ThrowIfTrue(ErrorCodes.NickTaken);

            //if the character is deleted and belongs to the issuer then it's ok
            bool b = owner.AccountId != issuerAccount.Id;
            if (b && (DateTime.Now.Subtract(owner.DeletedAt).TotalDays <= 1))
            {
                throw new PerpetuumException(ErrorCodes.NickTaken);
            }

            //there is a deleted character with the given nick which belongs to this account
            owner.Nick = nick + "_renamed_" + FastRandom.NextString(7);
        }

        [UsedImplicitly]
        public static IEnumerable<Character> GetCharactersDockedInBase(long baseEid)
        {
            return Db.Query()
                .CommandText("select characterid from characters where docked=1 and baseEID=@baseEID and active=1 and inUse=1")
                .SetParameter("@baseEID", baseEid)
                .Execute().Select(r => Get(r.GetValue<int>(0)));
        }

        [UsedImplicitly]
        public static Character GetByNick(string nick)
        {
            int id = Db.Query()
                .CommandText("select characterid from characters where nick = @nick")
                .SetParameter("@nick", nick)
                .ExecuteScalar<int>();

            return Get(id);
        }

        #endregion

        public void LogTransaction(TransactionLogEventBuilder builder)
        {
            LogTransaction(builder.Build());
        }

        public void LogTransaction(TransactionLogEvent e)
        {
            transactionLogger.Log(e);
        }

        public IDictionary<string, object> GetTransactionHistory(int offsetInDays)
        {
            DateTime later = DateTime.Now.AddDays(-offsetInDays);
            DateTime earlier = later.AddDays(-2);

            const string sqlCmd = @"SELECT transactionType,
                                           amount,
                                           transactiondate as date,
                                           currentcredit as credit,
                                           otherCharacter,
                                           quantity,
                                           definition
                                    FROM charactertransactions 
                                    WHERE characterid = @characterId AND transactiondate between @earlier AND @later and amount != 0";

            Dictionary<string, object> result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@characterId", Id)
                .SetParameter("@earlier", earlier)
                .SetParameter("@later", later)
                .Execute()
                .RecordsToDictionary("c");

            return result;
        }

        public bool IsInTraining()
        {
            return RaceId == 0 || SchoolId == 0;
        }

        public void CheckNextAvailableUndockTimeAndThrowIfFailed()
        {
            DateTime nextAvailableUndockTime = NextAvailableUndockTime;
            _ = nextAvailableUndockTime.ThrowIfGreater(
                DateTime.Now,
                ErrorCodes.DockingTimerStillRunning,
                gex => gex.SetData("nextAvailable", nextAvailableUndockTime));
        }

        public void SendErrorMessage(Command command, ErrorCodes error)
        {
            CreateErrorMessage(command, error).Send();
        }

        public MessageBuilder CreateErrorMessage(Command command, ErrorCodes error)
        {
            return Message.Builder.SetCommand(command).ToCharacter(this).WithError(error);
        }

        public void CheckPrivilegedTransactionsAndThrowIfFailed()
        {
            _ = IsPrivilegedTransactionsAllowed().ThrowIfError();
        }

        public ErrorCodes IsPrivilegedTransactionsAllowed()
        {
            AccessLevel accessLevel = AccessLevel;
            return !accessLevel.IsAnyPrivilegeSet()
                ? ErrorCodes.NoError
                : !accessLevel.IsAdminOrGm()
                    ? ErrorCodes.AccessDenied
                    : ErrorCodes.NoError;
        }

        public bool IsRobotSelectedForOtherCharacter(long robotEid)
        {
            int selectCheck = Db.Query()
                .CommandText("select count(*) from characters where activechassis=@robotEID and characterID != @characterID")
                .SetParameter("@characterID", Id).SetParameter("@robotEID", robotEid)
                .ExecuteNonQuery();

            if (selectCheck <= 0)
            {
                return false;
            }

            Logger.Error($"An evil attempt to select a robot twice happened. characterID:{Id} robotEID:{robotEid}");

            return true;
        }

        public IWallet<double> GetWalletWithAccessCheck(bool useCorporationWallet, TransactionType transactionType, params CorporationRole[] roles)
        {
            Character thisCharacter = this;

            return GetWallet(useCorporationWallet, transactionType, role =>
            {
                if (role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant))
                {
                    return true;
                }

                if (!role.IsAnyRole(CorporationRole.CEO))
                {
                    _ = thisCharacter.corporationManager.IsInJoinOrLeave(thisCharacter).ThrowIfError();
                }

                return role.IsAnyRole(roles);
            });
        }

        public IWallet<double> GetWallet(bool useCorporationWallet, TransactionType transactionType, Predicate<CorporationRole> accessChecker = null)
        {
            if (!useCorporationWallet)
            {
                return GetWallet(transactionType);
            }

            PrivateCorporation privateCorporation = GetPrivateCorporationOrThrow();

            if (accessChecker != null)
            {
                CorporationRole role = privateCorporation.GetMemberRole(this);
                if (!accessChecker(role))
                {
                    throw new PerpetuumException(ErrorCodes.InsufficientPrivileges);
                }
            }

            return new CorporationWallet(privateCorporation);
        }

        public IWallet<double> GetWallet(TransactionType transactionType)
        {
            return walletHelper.GetWallet(this, transactionType);
        }

        public void TransferCredit(Character target, long amount)
        {
            walletHelper.TransferCredit(this, target, amount);
        }

        public void AddToWallet(TransactionType transactionType, double amount)
        {
            walletHelper.AddToWallet(this, transactionType, amount);
        }

        public void SubtractFromWallet(TransactionType transactionType, double amount)
        {
            walletHelper.SubtractFromWallet(this, transactionType, amount);
        }

        public ICharacterSocial GetSocial()
        {
            return socialService.GetCharacterSocial(this);
        }

        public IEnumerable<Extension> GetDefaultExtensions()
        {
            return extensionReader.GetCharacterDefaultExtensions(this);
        }

        public void ResetAllExtensions()
        {
            DeleteAllSpentPoints();

            //reset the actual extension levels
            _ = Db.Query()
                .CommandText(
@"DELETE ce FROM characterextensions ce
INNER JOIN extensions e
ON ce.extensionid = e.extensionid
WHERE characterid=@characterID AND e.active = 1 AND e.hidden = 0")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            //reset remove log
            _ = Db.Query()
                .CommandText("DELETE extensionremovelog WHERE characterid=@characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            //remove ram
            characterExtensions.Remove(this);
        }

        public void IncreaseExtensionLevel(int extensionId, int extensionLevel)
        {
            _ = Db.Query()
                .CommandText("dbo.increaseExtensionLevel")
                .SetParameter("@characterID", Id)
                .SetParameter("@extensionID", extensionId)
                .SetParameter("@extensionLevel", extensionLevel)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLExecutionError);

            Character thisCharacter = this;
            Transaction.Current.OnCommited(() => thisCharacter.characterExtensions.Remove(thisCharacter));
        }

        public int GetExtensionLevel(int extensionId)
        {
            return GetExtensions().GetLevel(extensionId);
        }

        public CharacterExtensionCollection GetExtensions()
        {
            return characterExtensions.Get(this);
        }

        public void SetAllExtensionLevel(int level)
        {
            IEnumerable<Extension> extensions = extensionReader
                .GetExtensions()
                .Values
                .Where(e => !e.hidden)
                .Select(e => new Extension(e.id, level));
            SetExtensions(extensions);
        }

        public void SetExtensions(IEnumerable<Extension> extensions)
        {
            foreach (Extension extension in extensions)
            {
                SetExtension(extension);
            }
        }

        public void SetExtension(Extension extension)
        {
            Logger.Info($"extid:{extension.id} level:{extension.level}");

            if (extensionReader.GetExtensionByID(extension.id) == null)
            {
                Logger.Error($">>>> !!!!!!! >>>>>   extension not exists: {extension}");

                return;
            }

            _ = Db.Query().CommandText("dbo.setExtensionLevel")
                .SetParameter("@characterID", Id)
                .SetParameter("@extensionID", extension.id)
                .SetParameter("@extensionLevel", extension.level)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLExecutionError);

            Character tmpCharacter = this;
            Transaction.Current.OnCommited(() => tmpCharacter.characterExtensions.Remove(tmpCharacter));
        }

        public bool CheckLearnedExtension(Extension extension)
        {
            return GetExtensions().GetLevel(extension.id) >= extension.level;
        }

        public double GetExtensionsBonusSummary(params string[] extensionNames)
        {
            return GetExtensionsBonusSummary(extensionReader.GetExtensionIDsByName(extensionNames));
        }

        public double GetExtensionsBonusSummary(IEnumerable<int> extensionIDs)
        {
            Character thisC = this;

            return GetExtensions().SelectById(extensionIDs).Sum(e => e.level * thisC.extensionReader.GetExtensionByID(e.id).bonus);
        }

        public double GetExtensionBonusByName(string extensionName)
        {
            return GetExtensionBonus(extensionReader.GetExtensionIDByName(extensionName));
        }

        public double GetExtensionBonus(int extensionId)
        {
            return GetExtensions().GetLevel(extensionId) * extensionReader.GetExtensionByID(extensionId).bonus;
        }

        public int GetExtensionLevelSummaryByName(params string[] extensionNames)
        {
            CharacterExtensionCollection ex = GetExtensions();

            return extensionReader.GetExtensionIDsByName(extensionNames).Sum(extensionId => ex.GetLevel(extensionId));
        }

        public double GetExtensionBonusWithPrerequiredExtensions(string extensionName)
        {
            return GetExtensionBonusWithPrerequiredExtensions(extensionReader.GetExtensionIDByName(extensionName));
        }

        public double GetExtensionBonusWithPrerequiredExtensions(int extensionId)
        {
            return GetExtensionsBonusSummary(extensionReader.GetExtensionPrerequireTree(extensionId).Distinct());
        }

        public bool IsFriend(Character otherCharacter)
        {
            return GetSocial().GetFriendSocialState(otherCharacter) == SocialState.Friend;
        }

        public bool IsBlocked(Character otherCharacter)
        {
            return GetSocial().GetFriendSocialState(otherCharacter) == SocialState.Blocked;
        }

        [Pure]
        public int AddExtensionPointsBoostAndLog(EpForActivityType activityType, int points)
        {
            if (points <= 0)
            {
                return 0;
            }

            Account account = GetAccount();
            Debug.Assert(account != null, "account != null");

            return accountManager.AddExtensionPointsBoostAndLog(account, this, activityType, points);
        }


        [CanBeNull]
        public Task ReloadContainerOnZoneAsync()
        {
            Character character = this;

            return Task.Run(() => character.ReloadContainerOnZone());
        }

        public void ReloadContainerOnZone()
        {
            GetPlayerRobotFromZone()?.ReloadContainer();
        }

        public PublicContainer GetPublicContainer()
        {
            return GetCurrentDockingBase().GetPublicContainer();
        }

        public PublicContainer GetPublicContainerWithItems()
        {
            return GetCurrentDockingBase().GetPublicContainerWithItems(this);
        }

        public void SendItemErrorMessage(Command command, ErrorCodes error, Item item)
        {
            CreateErrorMessage(command, error)
                .SetExtraInfo(d =>
                {
                    d["eid"] = item.Eid;
                    d["name"] = item.ED.Name;
                }).Send();
        }

        public bool TechTreeNodeUnlocked(int definition)
        {
            if (techTreeService.GetUnlockedNodes(Eid).Any(n => n.Definition == definition))
            {
                return true;
            }

            bool hasRole = Corporation.GetRoleFromSql(this).HasRole(PresetCorporationRoles.CAN_LIST_TECHTREE);

            return hasRole && techTreeService.GetUnlockedNodes(CorporationEid).Any(n => n.Definition == definition);
        }

        public bool HasTechTreeBonus(int definition)
        {
            bool hasRole = Corporation.GetRoleFromSql(this).HasRole(PresetCorporationRoles.CAN_LIST_TECHTREE);
            return hasRole &&
                techTreeService.GetUnlockedNodes(Eid).Any(n => n.Definition == definition) &&
                techTreeService.GetUnlockedNodes(CorporationEid).Any(n => n.Definition == definition);
        }

        public DockingBase GetHomeBaseOrCurrentBase()
        {
            long resultBaseEid = HomeBaseEid ?? 0L;
            bool wasHomeBase = true;

            if (resultBaseEid == 0)
            {
                resultBaseEid = CurrentDockingBaseEid;
                wasHomeBase = false;
            }

            DockingBase dockingBase = dockingBaseHelper.GetDockingBase(resultBaseEid);
            if (dockingBase != null && dockingBase.IsDockingAllowed(this) == ErrorCodes.NoError)
            {
                return dockingBaseHelper.GetDockingBase(resultBaseEid);
            }

            //docking would normally fail
            if (wasHomeBase)
            {
                //was homebase set, clear it
                HomeBaseEid = null;
            }

            //pick the race related homebase
            resultBaseEid = DefaultCorporation.GetDockingBaseEid(this);

            Character thisCharacter = this;
            //inform dead player about this state
            Transaction.Current.OnCommited(() =>
            {
                Dictionary<string, object> info = new Dictionary<string, object>
                {
                    {k.characterID,thisCharacter.Id},
                    {k.baseEID, resultBaseEid},
                    {k.wasDeleted, wasHomeBase},
                };

                Message.Builder.SetCommand(Commands.CharacterForcedToBase).WithData(info).ToCharacter(thisCharacter).Send();
            });

            return dockingBaseHelper.GetDockingBase(resultBaseEid);
        }

        public void WriteItemTransactionLog(TransactionType transactionType, Item item)
        {
            TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                .SetTransactionType(transactionType)
                .SetCharacter(this)
                .SetItem(item.Definition, item.Quantity);
            LogTransaction(b);
        }

        public void SetActiveRobot(Robot robot)
        {
            ActiveRobotEid = robot?.Eid ?? 0L;

            Character thisCharacter = this;
            Dictionary<string, object> result = robot?.ToDictionary();
            Transaction.Current.OnCommited(() => Message.Builder.SetCommand(Commands.RobotActivated)
                .WithData(result)
                .WrapToResult()
                .ToCharacter(thisCharacter)
                .Send());
        }

        public void CleanGameRelatedData()
        {
            //corp founder
            _ = Db.Query()
                .CommandText("update corporations set founder=NULL where founder=@characterId")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            _ = Db.Query()
                .CommandText("delete characterextensions where characterid=@characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            _ = Db.Query()
                .CommandText("delete charactersettings where characterid=@characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            _ = Db.Query()
                .CommandText("delete charactersparks where characterid=@characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            _ = Db.Query()
                .CommandText("delete charactersparkteleports where characterid=@characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            _ = Db.Query()
                .CommandText("delete from channelmembers where memberid = @characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();

            TransportAssignment.CharacterDeleted(this);
        }

        [CanBeNull]
        public Robot GetActiveRobot()
        {
            return robotHelper.LoadRobotForCharacter(ActiveRobotEid, this, true);
        }

        [CanBeNull]
        public Gang GetGang()
        {
            return gangManager.GetGangByMember(this);
        }

        [CanBeNull]
        public Alliance GetAlliance()
        {
            long allianceEid = AllianceEid;

            return allianceEid == 0L ? null : Alliance.GetOrThrow(allianceEid);
        }

        public bool IsRobotSelectedForCharacter(Robot robot)
        {
            return ActiveRobotEid == robot?.Eid;
        }

        public DockingBase GetCurrentDockingBase()
        {
            long baseEid = CurrentDockingBaseEid;

            return dockingBaseHelper.GetDockingBase(baseEid);
        }

        [CanBeNull]
        public Player GetPlayerRobotFromZone()
        {
            IZone zone = GetCurrentZone();

            return zone?.GetPlayer(this);
        }

        public IZone GetCurrentZone()
        {
            return zoneManager.Value.GetZone(ZoneId ?? -1);
        }

        public IZone GetZone(int zoneiwant)
        {
            return zoneManager.Value.GetZone(zoneiwant);
        }

        public ZoneConfiguration GetCurrentZoneConfiguration()
        {
            return GetCurrentZone()?.Configuration ?? ZoneConfiguration.None;
        }

        public IDictionary<string, object> GetFullProfile()
        {
            System.Data.IDataRecord record = Db.Query().CommandText("select * from characters where characterid = @characterid")
                .SetParameter("@characterid", Id)
                .ExecuteSingleRow().ThrowIfNull(ErrorCodes.CharacterNotFound);

            long currentBaseEid = record.GetValue<long>("baseEID");
            DockingBase dockingBase = dockingBaseHelper.GetDockingBase(currentBaseEid);
            bool isInTraining = record.GetValue<int>("raceID") == 0;

            Dictionary<string, object> profile = new Dictionary<string, object>(21)
            {
                {k.raceID, record.GetValue<int>("raceID")},
                {k.creation, record.GetValue<DateTime>("creation")},
                {k.nick, record.GetValue<string>("nick")},
                {k.moodMessage, record.GetValue<string>("moodMessage")},
                {k.credit, (long) record.GetValue<double>("credit")},
                {k.lastUsed, record.IsDBNull("lastused") ? (object) null : record.GetValue<DateTime>("lastused")},
                {k.rootEID, record.GetValue<long>("rootEID")},
                {k.totalMinsOnline, record.GetValue<int>("totalMinsOnline")},
                {k.activeChassis, record.IsDBNull("activeChassis") ? (object) null : record.GetValue<long>("activeChassis")},
                {k.baseEID, record.IsDBNull("baseEID") ? (object) null : record.GetValue<long>("baseEID")},
                {k.majorID, record.GetValue<int>("majorID")},
                {k.schoolID, record.GetValue<int>("schoolID")},
                {k.sparkID, record.GetValue<int>("sparkID")},
                {k.defaultCorporation, record.GetValue<long>("defaultcorporationEID")},
                {k.corporationEID, CorporationEid},
                {k.allianceEID, AllianceEid},
                {k.avatar, (GenxyString) record.GetValue<string>("avatar")},
                {k.lastLogOut, record.IsDBNull("lastLogOut") ? (object) null : record.GetValue<DateTime>("lastLogOut")},
                {k.language, record.GetValue<int>("language")},
                {k.zoneID, record.GetValue<int?>("zoneID")},
                {k.homeBaseEID, record.GetValue<long?>("homeBaseEID")},
                {k.blockTrades, record.GetValue<bool>("blockTrades")},
                {k.dockingBaseInfo, dockingBase?.GetDockingBaseDetails()},
                {k.isInTraining, isInTraining},
            };

            techTreeService.AddInfoToDictionary(Eid, profile);

            return profile;
        }

        public PrivateCorporation GetPrivateCorporationOrThrow()
        {
            return GetPrivateCorporation().ThrowIfNull(ErrorCodes.CharacterMustBeInPrivateCorporation);
        }

        [CanBeNull]
        public PrivateCorporation GetPrivateCorporation()
        {
            return GetCorporation() as PrivateCorporation;
        }

        public Corporation GetCorporation()
        {
            return Corporation.GetOrThrow(CorporationEid);
        }

        public DefaultCorporation GetDefaultCorporation()
        {
            long eid = DefaultCorporationEid;
            return (DefaultCorporation)Corporation.GetOrThrow(eid);
        }

        public void DeleteAllSpentPoints()
        {
            //delete the spent points
            _ = Db.Query().CommandText("delete accountextensionspent where characterid=@characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();
        }

        public void GetTableIndexForAccountExtensionSpent(int extensionId, int extensionLevel, ref int spentId, ref int spentPoints)
        {
            System.Data.IDataRecord record = Db.Query().CommandText("select top 1 id,points from accountextensionspent where extensionid=@extensionID and extensionlevel=@extensionLevel and characterid=@characterID and points > 0 order by eventtime desc")
                .SetParameter("@extensionLevel", extensionLevel)
                .SetParameter("@extensionID", extensionId)
                .SetParameter("@characterID", Id)
                .ExecuteSingleRow();

            if (record == null)
            {
                return;
            }

            spentId = record.GetValue<int>(0);
            spentPoints = record.GetValue<int>(1);

            Debug.Assert(spentPoints > 0, "extension fuckup error");
        }
    }
}