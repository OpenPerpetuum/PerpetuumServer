using Perpetuum.Data;
using Perpetuum.GenXY;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterProfileRepository : ICharacterProfileRepository
    {
        public CharacterProfile Get(int characterID)
        {
            IDataRecord record = Db.Query().CommandText("select * from characters where characterid=@characterid")
                .SetParameter("@characterid", characterID)
                .ExecuteSingleRow();

            return record == null ? null : CreateCharacterProfileFromRecord(record);
        }

        private static CharacterProfile CreateCharacterProfileFromRecord(IDataRecord r)
        {
            CharacterProfile profile = new CharacterProfile
            {
                character = Character.Get(r.GetValue<int>("characterid")),
                accountID = r.GetValue<int>("accountid"),
                rootEID = r.GetValue<long>("rooteid"),
                nick = r.GetValue<string>("nick"),
                creation = r.GetValue<DateTime>("creation"),
                defaultCorporation = r.GetValue<long>("defaultcorporationeid"),
                avatar = (GenxyString)r.GetValue<string>("avatar"),
                raceID = r.GetValue<int>("raceid"),
                majorID = r.GetValue<int>("majorid"),
                schoolID = r.GetValue<int>("schoolid"),
                sparkID = r.GetValue<int>("sparkid"),
                moodMessage = r.GetValue<string>("moodmessage"),
                lastLoggedIn = r.GetValue<DateTime>("lastused"),
                lastLogOut = r.GetValue<DateTime>("lastlogout"),
                totalMinsOnline = r.GetValue<int>("totalminsonline"),
                language = r.GetValue<int>("language"),
                blockTrades = r.GetValue<bool>("blockTrades"),
                globalMute = r.GetValue<bool>("globalMute"),
                corporationEid = r.GetValue<long>("corporationeid"),
                allianceEid = r.GetValue<long?>("allianceeid"),
            };

            return profile;
        }

        public IEnumerable<CharacterProfile> GetAll()
        {
            return Db.Query()
                .CommandText("select * from characters")
                .Execute()
                .Select(CreateCharacterProfileFromRecord)
                .ToArray();
        }


        public IEnumerable<CharacterProfile> GetAllByAccount(Account account)
        {
            return Db.Query()
                .CommandText("Select * from characters where accountID=@accountId")
                .SetParameter("accountId", account.Id)
                .Execute()
                .Select(CreateCharacterProfileFromRecord)
                .ToArray();
        }
    }
}