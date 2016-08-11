﻿#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.Common;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class LevelUpSpecificPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId)
        {
            var all = await session.Inventory.GetPokemons();
            var pokemons = all.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax);
            var pokemon = pokemons.FirstOrDefault(p => p.Id == pokemonId);
            if (pokemon == null) return;
            var upgradeResult = await session.Inventory.UpgradePokemon(pokemon.Id);

            var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
            var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
            var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
            var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);            

            if (upgradeResult.Result == POGOProtos.Networking.Responses.UpgradePokemonResponse.Types.Result.Success)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeSuccess, session.Translation.GetPokemonName(upgradeResult.UpgradedPokemon.PokemonId), upgradeResult.UpgradedPokemon.Cp)
                });
                session.EventDispatcher.Send(new PokemonStatsChangedEvent()
                {
                    Uid = pokemonId,
                    Id = pokemon.PokemonId,
                    Family = family.FamilyId,
                    Candy = family.Candy_,
                    Cp = upgradeResult.UpgradedPokemon.Cp,
                    Iv = upgradeResult.UpgradedPokemon.CalculatePokemonPerfection()
                });
                
            }
            else if (upgradeResult.Result == POGOProtos.Networking.Responses.UpgradePokemonResponse.Types.Result.ErrorInsufficientResources)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailed)
                });
            }
            // pokemon max level
            else if (upgradeResult.Result == POGOProtos.Networking.Responses.UpgradePokemonResponse.Types.Result.ErrorUpgradeNotAvailable)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeUnavailable, session.Translation.GetPokemonName(pokemon.PokemonId), pokemon.Cp, PokemonInfo.CalculateMaxCp(pokemon))
                });
            }
            else
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailedError, session.Translation.GetPokemonName(pokemon.PokemonId))
                });
            }
            await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 2000);
        }
    }
}