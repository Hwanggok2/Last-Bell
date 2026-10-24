using System;
using System.Collections.Generic;

namespace LastBell
{
    public sealed partial class GameSession
    {
        private long StatUpgradeCost(int index)
        {
            if (index < 0 || index >= 5 || CurrentCharacter.statLevels[index] >= catalog.growth.maxLevel) return 0;
            return catalog.GetStatUpgradeCost(index,CurrentCharacter.statLevels[index] + 1);
        }

        private bool UpgradeStat(int index)
        {
            if (state.phase != SessionPhase.Lobby) return Reject("로비에서 강화할 수 있습니다.");
            long cost = StatUpgradeCost(index);
            if (cost == 0) return Reject("강화할 수 없는 능력치입니다.");
            if (state.gold >= cost)
            {
                state.gold -= cost;
                state.walletVersion++;
                CurrentCharacter.statLevels[index]++;
                LastMessage = "강화했습니다.";
            }
            else
            {
                long rate = ExchangeRate;
                int fragments = RequiredFragments(cost - state.gold, rate);
                state.exchange = new ExchangeQuoteData
                {
                    characterId = state.selectedCharacterId, statIndex = index, level = GetStatLevel(index),
                    upgradeCost = cost, fragments = fragments, gold = checked(fragments * rate),
                    exchangeRate = rate, walletGold = state.gold, walletFragments = state.fragments,
                    walletVersion = state.walletVersion
                };
                state.phase = SessionPhase.ExchangeConfirmation;
                LastMessage = state.fragments < fragments ? "종 파편이 부족합니다." : "교환 후 +를 다시 누르면 강화합니다.";
            }
            Publish();
            return true;
        }

        private static int RequiredFragments(long missing,long rate)
        {
            return checked((int)(missing/rate + (missing%rate == 0 ? 0 : 1)));
        }
        public bool RequestPurchaseExchange(long cost)
        {
            if(Phase!=SessionPhase.Lobby||cost<=Gold||cost<=0)return false;
            int fragments=RequiredFragments(cost-Gold,ExchangeRate);
            state.exchange=new ExchangeQuoteData{purchaseOnly=true,characterId=CurrentCharacterId,statIndex=-1,upgradeCost=cost,fragments=fragments,gold=checked(fragments*ExchangeRate),exchangeRate=ExchangeRate,walletGold=Gold,walletFragments=Fragments,walletVersion=state.walletVersion};
            state.phase=SessionPhase.ExchangeConfirmation;Publish();return true;
        }

        private bool IsQuoteCurrent()
        {
            var quote = state.exchange;
            if(quote!=null&&quote.purchaseOnly)return quote.characterId==CurrentCharacterId&&quote.walletVersion==state.walletVersion&&quote.walletGold==Gold&&quote.walletFragments==Fragments&&quote.upgradeCost>Gold&&quote.exchangeRate==ExchangeRate&&quote.fragments==RequiredFragments(quote.upgradeCost-Gold,ExchangeRate)&&quote.gold==quote.fragments*quote.exchangeRate;
            return quote != null && quote.characterId == state.selectedCharacterId && quote.statIndex >= 0 && quote.statIndex < 5 &&
                quote.level == GetStatLevel(quote.statIndex) && quote.upgradeCost == StatUpgradeCost(quote.statIndex) &&
                quote.walletVersion == state.walletVersion && quote.walletGold == state.gold && quote.walletFragments == state.fragments &&
                quote.exchangeRate == ExchangeRate && quote.upgradeCost > state.gold &&
                quote.fragments == RequiredFragments(quote.upgradeCost - state.gold, quote.exchangeRate) &&
                quote.gold == quote.fragments * quote.exchangeRate;
        }

        private bool CommitExchange()
        {
            if (state.phase != SessionPhase.ExchangeConfirmation || state.exchange == null) return false;
            if (!IsQuoteCurrent())
            {
                state.exchange = null;
                state.phase = SessionPhase.Lobby;
                LastMessage = "견적이 바뀌었습니다. +를 눌러 다시 확인하세요.";
                Publish();
                return false;
            }
            var quote = state.exchange;
            if (state.fragments < quote.fragments) return Reject("종 파편이 부족합니다.");
            long newGold = checked(state.gold + quote.gold);
            state.fragments -= quote.fragments;
            state.gold = newGold;
            state.walletVersion++;
            state.exchange = null;
            state.phase = SessionPhase.Lobby;
            LastMessage = "골드를 교환했습니다. +를 눌러 강화하세요.";
            Publish();
            return true;
        }

        private bool DismissExchange()
        {
            if (state.phase != SessionPhase.ExchangeConfirmation) return false;
            state.exchange = null;
            state.phase = SessionPhase.Lobby;
            LastMessage = "";
            Publish();
            return true;
        }

        private bool ChangeCharacter(string id)
        {
            if (state.phase != SessionPhase.Lobby && state.phase != SessionPhase.ExchangeConfirmation) return false;
            var definition = Array.Find(catalog.characters, x => x.id == id);
            if (definition == null || !definition.availableInMvp) return Reject("아직 제작 중인 캐릭터입니다.");
            state.selectedCharacterId = id;
            state.exchange = null;
            state.phase = SessionPhase.Lobby;
            LastMessage = "";
            Publish();
            return true;
        }

        private bool PurchaseSlot()
        {
            if (state.phase != SessionPhase.Lobby) return false;
            int cost = NextStorageExpansionCost;
            if (cost <= 0) return Reject("보관함을 최대로 확장했습니다.");
            if (state.fragments < cost) return Reject("종 파편이 부족합니다.");
            state.fragments -= cost;
            state.walletVersion++;
            state.storageCapacity++;
            LastMessage = "유물 보관함을 확장했습니다.";
            Publish();
            return true;
        }
    }
}
