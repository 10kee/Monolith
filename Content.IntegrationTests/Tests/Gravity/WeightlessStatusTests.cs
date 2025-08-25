// SPDX-FileCopyrightText: 2020 DamianX
// SPDX-FileCopyrightText: 2020 chairbender
// SPDX-FileCopyrightText: 2021 DrSmugleaf
// SPDX-FileCopyrightText: 2021 Javier Guardia Fernández
// SPDX-FileCopyrightText: 2021 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto
// SPDX-FileCopyrightText: 2021 metalgearsloth
// SPDX-FileCopyrightText: 2022 Acruid
// SPDX-FileCopyrightText: 2022 mirrorcult
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2023 Leon Friedrich
// SPDX-FileCopyrightText: 2023 TemporalOroboros
// SPDX-FileCopyrightText: 2023 Visne
// SPDX-FileCopyrightText: 2024 Julian Giebel
// SPDX-FileCopyrightText: 2024 Nemanja
// SPDX-FileCopyrightText: 2025 Redrover1760
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Gravity;
using Content.Shared.Alert;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(GravitySystem))]
    [TestOf(typeof(GravityGeneratorComponent))]
    public sealed class WeightlessStatusTests
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanWeightlessDummy
  id: HumanWeightlessDummy
  components:
  - type: Alerts
  - type: Physics
    bodyType: Dynamic

- type: entity
  name: WeightlessGravityGeneratorDummy
  id: WeightlessGravityGeneratorDummy
  components:
  - type: GravityGenerator
  - type: PowerCharge
    windowTitle: gravity-generator-window-title
    idlePower: 50
    chargeRate: 1000000000 # Set this really high so it discharges in a single tick.
    activePower: 500
  - type: ApcPowerReceiver
    needsPower: false
  - type: UserInterface
";
        [Test]
        public async Task WeightlessStatusTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var alertsSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AlertsSystem>();
            var weightlessAlert = SharedGravitySystem.WeightlessAlert;

            EntityUid human = default;

            var testMap = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity("HumanWeightlessDummy", testMap.GridCoords);

                Assert.That(entityManager.TryGetComponent(human, out AlertsComponent alerts));
            });

            // Let WeightlessSystem and GravitySystem tick
            await pair.RunTicksSync(30);
            var generatorUid = EntityUid.Invalid;
            await server.WaitAssertion(() =>
            {
                // No gravity without a gravity generator
                Assert.That(alertsSystem.IsShowingAlert(human, weightlessAlert));

                generatorUid = entityManager.SpawnEntity("WeightlessGravityGeneratorDummy", entityManager.GetComponent<TransformComponent>(human).Coordinates);
            });

            // Let WeightlessSystem and GravitySystem tick
            await pair.RunTicksSync(30);

            await server.WaitAssertion(() =>
            {
                Assert.That(alertsSystem.IsShowingAlert(human, weightlessAlert), Is.False);

                // This should kill gravity
                entityManager.DeleteEntity(generatorUid);
            });

            await pair.RunTicksSync(30);

            await server.WaitAssertion(() =>
            {
                Assert.That(alertsSystem.IsShowingAlert(human, weightlessAlert));
            });

            await pair.RunTicksSync(30);

            await pair.CleanReturnAsync();
        }
    }
}
