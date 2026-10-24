using System;
namespace LastBell
{
    public sealed class UpgradeQuote
    {
        public string CharacterId {get;internal set;}
        public int StatIndex {get;internal set;}
        public int FromLevel {get;internal set;}
        public int ToLevel {get;internal set;}
        public long Cost {get;internal set;}
        public long Wallet {get;internal set;}
        public long WalletVersion {get;internal set;}
        public CombatStatsData Before {get;internal set;}
        public CombatStatsData After {get;internal set;}
    }
    public sealed partial class GameSession
    {
        public UpgradeQuote QuoteUpgrade(int index,bool maximum)
        {
            if(index<0||index>4)return null;
            int from=GetStatLevel(index),to=from;long cost=0;
            while(to<catalog.growth.maxLevel)
            {
                long next=catalog.GetStatUpgradeCost(index,to+1);
                if(maximum&&next>state.gold-cost)break;
                cost=checked(cost+next);to++;
                if(!maximum)break;
            }
            var levels=(int[])CurrentCharacter.statLevels.Clone();levels[index]=to;
            return new UpgradeQuote{CharacterId=CurrentCharacterId,StatIndex=index,FromLevel=from,ToLevel=to,Cost=cost,Wallet=Gold,WalletVersion=state.walletVersion,
                Before=BuildLobbyStats(),After=LobbyStatsFor(CurrentCharacter,levels)};
        }
        public bool PurchaseUpgrade(UpgradeQuote quote)
        {
            if(Phase!=SessionPhase.Lobby||quote==null||quote.StatIndex<0||quote.StatIndex>4)return false;
            if(quote.CharacterId!=CurrentCharacterId||quote.Wallet!=Gold||quote.WalletVersion!=state.walletVersion||quote.FromLevel!=GetStatLevel(quote.StatIndex))
                return Reject("견적이 바뀌었습니다. 다시 확인하세요.");
            long cost=0;
            if(quote.ToLevel<=quote.FromLevel||quote.ToLevel>catalog.growth.maxLevel)return Reject("강화할 수 없습니다.");
            for(int level=quote.FromLevel+1;level<=quote.ToLevel;level++)cost=checked(cost+catalog.GetStatUpgradeCost(quote.StatIndex,level));
            if(cost!=quote.Cost||cost>Gold)return Reject("골드가 부족합니다.");
            state.gold-=cost;state.walletVersion++;CurrentCharacter.statLevels[quote.StatIndex]=quote.ToLevel;
            LastMessage="강화했습니다.";Publish();return true;
        }
        public bool RequestUpgradeExchange(int index)
        {
            if(Phase!=SessionPhase.Lobby||StatUpgradeCost(index)<=Gold)return false;
            return UpgradeStat(index);
        }
        public CombatStatsData CharacterStats(string id)
        {
            var progress=state.characters.Find(x=>x.id==id);
            return LobbyStatsFor(progress,progress.statLevels);
        }
    }
}
