using System;
using System.Collections.Generic;

namespace LastBell
{
    public sealed partial class GameSession
    {
        private int StatUpgradeCost(int index)
        {
            if (index < 0 || index >= 4 || CurrentCharacter.statLevels[index] >= catalog.growth.maxLevel) return 0;
            return catalog.GetUpgradeCost(CurrentCharacter.statLevels[index] + 1);
        }

        private bool UpgradeStat(int index)
        {
            if (state.phase != SessionPhase.Lobby) return Reject("로비에서 강화할 수 있습니다.");
            int cost = StatUpgradeCost(index);
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
                int rate = catalog.economy.exchangeGoldPerFragment;
                int fragments = (cost - state.gold + rate - 1) / rate;
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

        private bool IsQuoteCurrent()
        {
            var quote = state.exchange;
            return quote != null && quote.characterId == state.selectedCharacterId && quote.statIndex >= 0 && quote.statIndex < 4 &&
                quote.level == GetStatLevel(quote.statIndex) && quote.upgradeCost == StatUpgradeCost(quote.statIndex) &&
                quote.walletVersion == state.walletVersion && quote.walletGold == state.gold && quote.walletFragments == state.fragments &&
                quote.exchangeRate == catalog.economy.exchangeGoldPerFragment && quote.upgradeCost > state.gold &&
                quote.fragments == (quote.upgradeCost - state.gold + quote.exchangeRate - 1) / quote.exchangeRate &&
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
            int newGold = checked(state.gold + quote.gold);
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

        private bool ChangeEquipment(string id, bool equip)
        {
            if (state.phase != SessionPhase.Lobby) return Reject("장착은 로비에서 변경하세요.");
            if (FindRelicLevel(id) == 0) return Reject("아직 보유하지 않은 유물입니다.");
            var list = new List<string>(CurrentCharacter.equippedRelicIds);
            if (equip)
            {
                if (list.Contains(id)) return false;
                if (list.Count >= state.unlockedSlots) return Reject("장착 슬롯이 가득 찼습니다.");
                list.Add(id);
            }
            else if (!list.Remove(id)) return false;
            CurrentCharacter.equippedRelicIds = list.ToArray();
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
            int index = state.unlockedSlots - 1;
            if (state.unlockedSlots >= catalog.relicRules.maxEquipped || index >= catalog.economy.slotCosts.Length)
                return Reject("모든 슬롯을 열었습니다.");
            if (state.highestClearedStage < catalog.economy.slotUnlockStages[index])
                return Reject(catalog.economy.slotUnlockStages[index] + "층 완료 후 구매할 수 있습니다.");
            int cost = catalog.economy.slotCosts[index];
            if (state.fragments < cost) return Reject("종 파편이 부족합니다.");
            state.fragments -= cost;
            state.walletVersion++;
            state.unlockedSlots++;
            LastMessage = "공용 장착 슬롯을 열었습니다.";
            Publish();
            return true;
        }
    }
}
