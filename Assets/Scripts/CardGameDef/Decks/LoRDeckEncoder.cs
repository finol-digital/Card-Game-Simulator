// https://github.com/RiotGames/LoRDeckCodes/blob/main/LoRDeckCodes/LoRDeckEncoder.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGameDef.Decks
{
    public static class LoRDeckEncoder
    {
        private const int CardCodeLength = 7;
        private static readonly Dictionary<string, int> FactionCodeToIntIdentifier = new Dictionary<string, int>();
        private static readonly Dictionary<int, string> IntIdentifierToFactionCode = new Dictionary<int, string>();
        private const int MaxKnownVersion = 2;

        static LoRDeckEncoder()
        {
            FactionCodeToIntIdentifier.Add("DE", 0);
            FactionCodeToIntIdentifier.Add("FR", 1);
            FactionCodeToIntIdentifier.Add("IO", 2);
            FactionCodeToIntIdentifier.Add("NX", 3);
            FactionCodeToIntIdentifier.Add("PZ", 4);
            FactionCodeToIntIdentifier.Add("SI", 5);
            FactionCodeToIntIdentifier.Add("BW", 6);
            IntIdentifierToFactionCode.Add(0, "DE");
            IntIdentifierToFactionCode.Add(1, "FR");
            IntIdentifierToFactionCode.Add(2, "IO");
            IntIdentifierToFactionCode.Add(3, "NX");
            IntIdentifierToFactionCode.Add(4, "PZ");
            IntIdentifierToFactionCode.Add(5, "SI");
            IntIdentifierToFactionCode.Add(6, "BW");
        }

        public static List<CardCodeAndCount> GetDeckFromCode(string code)
        {
            List<CardCodeAndCount> result = new List<CardCodeAndCount>();

            byte[] bytes;
            try
            {
                bytes = Base32.Decode(code);
            }
            catch
            {
                throw new ArgumentException("Invalid deck code");
            }

            List<byte> byteList = bytes.ToList();

            //grab format and version
            //*commented code removed*
            int version = bytes[0] & 0xF;
            byteList.RemoveAt(0);

            if (version > MaxKnownVersion)
            {
                throw new ArgumentException(
                    "The provided code requires a higher version of this library; please update.");
            }

            for (int i = 3; i > 0; i--)
            {
                int numGroupOfs = Varint.PopVarint(byteList);

                for (int j = 0; j < numGroupOfs; j++)
                {
                    int numOfsInThisGroup = Varint.PopVarint(byteList);
                    int set = Varint.PopVarint(byteList);
                    int faction = Varint.PopVarint(byteList);

                    for (int k = 0; k < numOfsInThisGroup; k++)
                    {
                        int card = Varint.PopVarint(byteList);

                        string setString = set.ToString().PadLeft(2, '0');
                        string factionString = IntIdentifierToFactionCode[faction];
                        string cardString = card.ToString().PadLeft(3, '0');

                        CardCodeAndCount newEntry = new CardCodeAndCount()
                            {CardCode = setString + factionString + cardString, Count = i};
                        result.Add(newEntry);
                    }
                }
            }

            //the remainder of the deck code is comprised of entries for cards with counts >= 4
            //this will only happen in Limited and special game modes.
            //the encoding is simply [count] [card code]
            while (byteList.Count > 0)
            {
                int fourPlusCount = Varint.PopVarint(byteList);
                int fourPlusSet = Varint.PopVarint(byteList);
                int fourPlusFaction = Varint.PopVarint(byteList);
                int fourPlusNumber = Varint.PopVarint(byteList);

                string fourPlusSetString = fourPlusSet.ToString().PadLeft(2, '0');
                string fourPlusFactionString = IntIdentifierToFactionCode[fourPlusFaction];
                string fourPlusNumberString = fourPlusNumber.ToString().PadLeft(3, '0');

                CardCodeAndCount newEntry = new CardCodeAndCount()
                {
                    CardCode = fourPlusSetString + fourPlusFactionString + fourPlusNumberString, Count = fourPlusCount
                };
                result.Add(newEntry);
            }

            return result;
        }

        public static string GetCodeFromDeck(List<CardCodeAndCount> deck)
        {
            string result = Base32.Encode(GetDeckCodeBytes(deck));
            return result;
        }

        private static byte[] GetDeckCodeBytes(List<CardCodeAndCount> deck)
        {
            List<byte> result = new List<byte>();

            if (!ValidCardCodesAndCounts(deck))
                throw new ArgumentException("The provided deck contains invalid card codes.");

            byte[] formatAndVersion = new byte[] {17}; //i.e. 00010001
            result.AddRange(formatAndVersion);

            List<CardCodeAndCount> of3 = new List<CardCodeAndCount>();
            List<CardCodeAndCount> of2 = new List<CardCodeAndCount>();
            List<CardCodeAndCount> of1 = new List<CardCodeAndCount>();
            List<CardCodeAndCount> ofN = new List<CardCodeAndCount>();

            foreach (CardCodeAndCount ccc in deck)
            {
                if (ccc.Count == 3)
                    of3.Add(ccc);
                else if (ccc.Count == 2)
                    of2.Add(ccc);
                else if (ccc.Count == 1)
                    of1.Add(ccc);
                else if (ccc.Count < 1)
                    throw new ArgumentException("Invalid count of " + ccc.Count + " for card " + ccc.CardCode);
                else
                    ofN.Add(ccc);
            }

            //build the lists of set and faction combinations within the groups of similar card counts
            List<List<CardCodeAndCount>> groupedOf3 = GetGroupedOfs(of3);
            List<List<CardCodeAndCount>> groupedOf2 = GetGroupedOfs(of2);
            List<List<CardCodeAndCount>> groupedOf1 = GetGroupedOfs(of1);

            //to ensure that the same deck list in any order produces the same code, do some sorting
            groupedOf3 = SortGroupOf(groupedOf3);
            groupedOf2 = SortGroupOf(groupedOf2);
            groupedOf1 = SortGroupOf(groupedOf1);

            //OfN (since rare) are simply sorted by the card code - there's no optimization based upon the card count
            ofN = ofN.OrderBy(c => c.CardCode).ToList();

            //Encode
            EncodeGroupOf(result, groupedOf3);
            EncodeGroupOf(result, groupedOf2);
            EncodeGroupOf(result, groupedOf1);

            //Cards with 4+ counts are handled differently: simply [count] [card code] for each
            EncodeNOfs(result, ofN);

            return result.ToArray();
        }

        private static void EncodeNOfs(List<byte> bytes, List<CardCodeAndCount> nOfs)
        {
            foreach (CardCodeAndCount ccc in nOfs)
            {
                bytes.AddRange(Varint.GetVarint((ulong) ccc.Count));

                ParseCardCode(ccc.CardCode, out int setNumber, out string factionCode, out int cardNumber);
                int factionNumber = FactionCodeToIntIdentifier[factionCode];

                bytes.AddRange(Varint.GetVarint((ulong) setNumber));
                bytes.AddRange(Varint.GetVarint((ulong) factionNumber));
                bytes.AddRange(Varint.GetVarint((ulong) cardNumber));
            }
        }

        //The sorting convention of this encoding scheme is
        //First by the number of set/faction combinations in each top-level list
        //Second by the alphanumeric order of the card codes within those lists.
        private static List<List<CardCodeAndCount>> SortGroupOf(List<List<CardCodeAndCount>> groupOf)
        {
            groupOf = groupOf.OrderBy(g => g.Count).ToList();
            for (int i = 0; i < groupOf.Count; i++)
            {
                groupOf[i] = groupOf[i].OrderBy(c => c.CardCode).ToList();
            }

            return groupOf;
        }

        private static void ParseCardCode(string code, out int set, out string faction, out int number)
        {
            set = int.Parse(code.Substring(0, 2));
            faction = code.Substring(2, 2);
            number = int.Parse(code.Substring(4, 3));
        }

        private static List<List<CardCodeAndCount>> GetGroupedOfs(List<CardCodeAndCount> list)
        {
            List<List<CardCodeAndCount>> result = new List<List<CardCodeAndCount>>();
            while (list.Count > 0)
            {
                List<CardCodeAndCount> currentSet = new List<CardCodeAndCount>();

                //get info from first
                string firstCardCode = list[0].CardCode;
                ParseCardCode(firstCardCode, out int setNumber, out string factionCode, out int _);

                //now add that to our new list, remove from old
                currentSet.Add(list[0]);
                list.RemoveAt(0);

                //sweep through rest of list and grab entries that should live with our first one.
                //matching means same set and faction - we are already assured the count matches from previous grouping.
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    string currentCardCode = list[i].CardCode;
                    int currentSetNumber = int.Parse(currentCardCode.Substring(0, 2));
                    string currentFactionCode = currentCardCode.Substring(2, 2);

                    if (currentSetNumber == setNumber && currentFactionCode == factionCode)
                    {
                        currentSet.Add(list[i]);
                        list.RemoveAt(i);
                    }
                }

                result.Add(currentSet);
            }

            return result;
        }

        private static void EncodeGroupOf(List<byte> bytes, List<List<CardCodeAndCount>> groupOf)
        {
            bytes.AddRange(Varint.GetVarint((ulong) groupOf.Count));
            foreach (List<CardCodeAndCount> currentList in groupOf)
            {
                //how many cards in current group?
                bytes.AddRange(Varint.GetVarint((ulong) currentList.Count));

                //what is this group, as identified by a set and faction pair
                string currentCardCode = currentList[0].CardCode;
                ParseCardCode(currentCardCode, out int currentSetNumber, out string currentFactionCode, out int _);
                int currentFactionNumber = FactionCodeToIntIdentifier[currentFactionCode];
                bytes.AddRange(Varint.GetVarint((ulong) currentSetNumber));
                bytes.AddRange(Varint.GetVarint((ulong) currentFactionNumber));

                //what are the cards, as identified by the third section of card code only now, within this group?
                foreach (CardCodeAndCount cd in currentList)
                {
                    string code = cd.CardCode;
                    int sequenceNumber = int.Parse(code.Substring(4, 3));
                    bytes.AddRange(Varint.GetVarint((ulong) sequenceNumber));
                }
            }
        }

        public static bool ValidCardCodesAndCounts(List<CardCodeAndCount> deck)
        {
            foreach (CardCodeAndCount ccc in deck)
            {
                if (ccc.CardCode.Length != CardCodeLength)
                    return false;

                if (!int.TryParse(ccc.CardCode.Substring(0, 2), out int _))
                    return false;

                string faction = ccc.CardCode.Substring(2, 2);
                if (!FactionCodeToIntIdentifier.ContainsKey(faction))
                    return false;

                if (!int.TryParse(ccc.CardCode.Substring(4, 3), out int _))
                    return false;

                if (ccc.Count < 1)
                    return false;
            }

            return true;
        }
    }
}
