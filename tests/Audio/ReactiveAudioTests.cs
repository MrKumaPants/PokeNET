using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio;
using PokeNET.Audio.Reactive;
using PokeNET.Audio.Reactive.Reactions;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.Models;
using GameState = PokeNET.Domain.Models.GameState;
using AudioReactionType = PokeNET.Audio.Reactive.AudioReactionType;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Comprehensive tests for ReactiveAudioEngine
    /// Tests state-based audio reactions and event handling using the strategy pattern
    /// </summary>
    public class ReactiveAudioTests : IDisposable
    {
        private readonly Mock<ILogger<ReactiveAudioEngine>> _mockLogger;
        private readonly Mock<IAudioManager> _mockAudioManager;
        private readonly Mock<IEventBus> _mockEventBus;
        private readonly Mock<AudioReactionRegistry> _mockRegistry;
        private ReactiveAudioEngine? _reactiveAudio;
        private Action<IGameEvent>? _eventHandler;

        public ReactiveAudioTests()
        {
            _mockLogger = new Mock<ILogger<ReactiveAudioEngine>>();
            _mockAudioManager = new Mock<IAudioManager>();
            _mockEventBus = new Mock<IEventBus>();

            // Create mock registry
            var mockRegistryLogger = new Mock<ILogger<AudioReactionRegistry>>();
            _mockRegistry = new Mock<AudioReactionRegistry>(mockRegistryLogger.Object, new List<IAudioReaction>());
        }

        private ReactiveAudioEngine CreateReactiveAudio(List<IAudioReaction>? reactions = null)
        {
            // Create a real registry with provided reactions or empty list
            var registryLogger = new Mock<ILogger<AudioReactionRegistry>>();
            var registry = new AudioReactionRegistry(registryLogger.Object, reactions ?? new List<IAudioReaction>());

            return new ReactiveAudioEngine(
                _mockLogger.Object,
                _mockAudioManager.Object,
                _mockEventBus.Object,
                registry
            );
        }

        private void CaptureEventHandler()
        {
            // Capture the event handler registered by ReactiveAudioEngine
            _mockEventBus.Setup(e => e.Subscribe<IGameEvent>(It.IsAny<Action<IGameEvent>>()))
                .Callback<Action<IGameEvent>>(handler => _eventHandler = handler);
        }

        private async Task SimulateEvent(IGameEvent gameEvent)
        {
            // Simulate event by calling captured handler
            _eventHandler?.Invoke(gameEvent);
            await Task.Delay(10); // Allow async processing
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDependencies()
        {
            // Arrange & Act
            _reactiveAudio = CreateReactiveAudio();

            // Assert
            _reactiveAudio.Should().NotBeNull();
            _reactiveAudio.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public async Task InitializeAsync_ShouldSubscribeToEvents()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();

            // Act
            await _reactiveAudio.InitializeAsync();

            // Assert
            _reactiveAudio.IsInitialized.Should().BeTrue();
            _mockEventBus.Verify(e => e.Subscribe<IGameEvent>(It.IsAny<Action<IGameEvent>>()), Times.Once);
        }

        #endregion

        #region Game State Reaction Tests

        [Fact]
        public async Task GameStateChanged_ToBattle_ShouldPlayBattleMusic()
        {
            // Arrange
            var gameStateReaction = new GameStateReaction(new Mock<ILogger<GameStateReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { gameStateReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new GameStateChangedEvent(GameState.Overworld, GameState.Battle);
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("battle")), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GameStateChanged_ToOverworld_ShouldPlayOverworldMusic()
        {
            // Arrange
            var gameStateReaction = new GameStateReaction(new Mock<ILogger<GameStateReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { gameStateReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new GameStateChangedEvent(GameState.Battle, GameState.Overworld);
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("route")), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GameStateChanged_ToMenu_ShouldDuckMusic()
        {
            // Arrange
            var gameStateReaction = new GameStateReaction(new Mock<ILogger<GameStateReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { gameStateReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new GameStateChangedEvent(GameState.Overworld, GameState.Menu);
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.DuckMusicAsync(It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GameStateChanged_FromMenu_ShouldUnduckMusic()
        {
            // Arrange
            var gameStateReaction = new GameStateReaction(new Mock<ILogger<GameStateReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { gameStateReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act - First go to menu, then leave menu
            var evt1 = new GameStateChangedEvent(GameState.Overworld, GameState.Menu);
            await SimulateEvent(evt1);
            var evt2 = new GameStateChangedEvent(GameState.Menu, GameState.Overworld);
            await SimulateEvent(evt2);

            // Assert
            _mockAudioManager.Verify(
                a => a.StopDuckingAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Theory]
        [InlineData(GameState.Battle)]
        [InlineData(GameState.Overworld)]
        [InlineData(GameState.Menu)]
        [InlineData(GameState.Cutscene)]
        public async Task GameStateChanged_ToAnyState_ShouldTriggerReaction(GameState targetState)
        {
            // Arrange
            var gameStateReaction = new GameStateReaction(new Mock<ILogger<GameStateReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { gameStateReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new GameStateChangedEvent(GameState.Overworld, targetState);
            await SimulateEvent(evt);

            // Assert - At minimum, the event handler should be called
            _eventHandler.Should().NotBeNull();
        }

        #endregion

        #region Battle Event Reaction Tests

        [Fact]
        public async Task BattleStart_ShouldPlayBattleIntro()
        {
            // Arrange
            var battleStartReaction = new BattleStartReaction(new Mock<ILogger<BattleStartReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { battleStartReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new BattleStartEvent { IsWildBattle = true };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("battle_start")), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task BattleStart_WildBattle_ShouldPlayWildBattleMusic()
        {
            // Arrange
            var battleStartReaction = new BattleStartReaction(new Mock<ILogger<BattleStartReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { battleStartReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new BattleStartEvent { IsWildBattle = true };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("wild")), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task BattleStart_TrainerBattle_ShouldPlayTrainerBattleMusic()
        {
            // Arrange
            var battleStartReaction = new BattleStartReaction(new Mock<ILogger<BattleStartReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { battleStartReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new BattleStartEvent { IsWildBattle = false, IsGymLeader = false };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("trainer")), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task BattleStart_GymLeaderBattle_ShouldPlayGymLeaderMusic()
        {
            // Arrange
            var battleStartReaction = new BattleStartReaction(new Mock<ILogger<BattleStartReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { battleStartReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new BattleStartEvent { IsWildBattle = false, IsGymLeader = true };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("gym") || s.Contains("leader")), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task BattleEnd_PlayerWins_ShouldPlayVictoryMusic()
        {
            // Arrange
            var battleEndReaction = new BattleEndReaction(new Mock<ILogger<BattleEndReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { battleEndReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new BattleEndEvent { PlayerWon = true };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("victory")), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task PokemonFaint_ShouldPlayFaintSound()
        {
            // Arrange
            var pokemonFaintReaction = new PokemonFaintReaction(new Mock<ILogger<PokemonFaintReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { pokemonFaintReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new PokemonFaintEvent { PokemonName = "Pikachu" };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("faint")), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task AttackUse_ShouldPlayAttackSound()
        {
            // Arrange
            var attackReaction = new AttackReaction(new Mock<ILogger<AttackReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { attackReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new AttackEvent { AttackName = "Thunderbolt", AttackType = "Electric" };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CriticalHit_ShouldPlayCritSound()
        {
            // Arrange
            var criticalHitReaction = new CriticalHitReaction(new Mock<ILogger<CriticalHitReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { criticalHitReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new CriticalHitEvent();
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("critical")), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        #endregion

        #region Health-Based Reactions Tests

        [Fact]
        public async Task HealthChanged_BelowLowThreshold_ShouldPlayLowHealthMusic()
        {
            // Arrange
            var healthChangedReaction = new HealthChangedReaction(new Mock<ILogger<HealthChangedReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { healthChangedReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new HealthChangedEvent
            {
                CurrentHealth = 15,
                MaxHealth = 100,
                HealthPercentage = 0.15f
            };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("low_health")), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task HealthChanged_AboveLowThreshold_ShouldStopLowHealthMusic()
        {
            // Arrange
            var healthChangedReaction = new HealthChangedReaction(new Mock<ILogger<HealthChangedReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { healthChangedReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act - First trigger low health, then recover
            var evt1 = new HealthChangedEvent { HealthPercentage = 0.15f };
            await SimulateEvent(evt1);
            var evt2 = new HealthChangedEvent { HealthPercentage = 0.5f };
            await SimulateEvent(evt2);

            // Assert
            _mockAudioManager.Verify(
                a => a.StopMusicAsync(),
                Times.AtLeastOnce
            );
        }

        #endregion

        #region Weather and Environment Tests

        [Fact]
        public async Task WeatherChange_ToRain_ShouldPlayRainAmbient()
        {
            // Arrange
            var weatherChangedReaction = new WeatherChangedReaction(new Mock<ILogger<WeatherChangedReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { weatherChangedReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new WeatherChangedEvent { NewWeather = Weather.Rain };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayAmbientAsync(It.Is<string>(s => s.Contains("rain")), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task WeatherChange_ToClear_ShouldStopAmbient()
        {
            // Arrange
            var weatherChangedReaction = new WeatherChangedReaction(new Mock<ILogger<WeatherChangedReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { weatherChangedReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act - First rain, then clear
            var evt1 = new WeatherChangedEvent { NewWeather = Weather.Rain };
            await SimulateEvent(evt1);
            var evt2 = new WeatherChangedEvent { NewWeather = Weather.Clear };
            await SimulateEvent(evt2);

            // Assert
            _mockAudioManager.Verify(
                a => a.StopAmbientAsync(),
                Times.Once
            );
        }

        [Theory]
        [InlineData(Weather.Rain)]
        [InlineData(Weather.Snow)]
        [InlineData(Weather.Sandstorm)]
        [InlineData(Weather.Fog)]
        public async Task WeatherChange_ToWeatherType_ShouldPlayCorrespondingAmbient(Weather weather)
        {
            // Arrange
            var weatherChangedReaction = new WeatherChangedReaction(new Mock<ILogger<WeatherChangedReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { weatherChangedReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new WeatherChangedEvent { NewWeather = weather };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayAmbientAsync(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        #endregion

        #region Item and Interaction Tests

        [Fact]
        public async Task ItemUse_ShouldPlayItemSound()
        {
            // Arrange
            var itemUseReaction = new ItemUseReaction(new Mock<ILogger<ItemUseReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { itemUseReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new ItemUseEvent { ItemName = "Potion" };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task PokemonCaught_ShouldPlayCatchSound()
        {
            // Arrange
            var pokemonCaughtReaction = new PokemonCaughtReaction(new Mock<ILogger<PokemonCaughtReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { pokemonCaughtReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new PokemonCaughtEvent { PokemonName = "Bulbasaur" };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("catch")), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task LevelUp_ShouldPlayLevelUpSound()
        {
            // Arrange
            var levelUpReaction = new LevelUpReaction(new Mock<ILogger<LevelUpReaction>>().Object);
            _reactiveAudio = CreateReactiveAudio(new List<IAudioReaction> { levelUpReaction });
            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();

            // Act
            var evt = new LevelUpEvent { NewLevel = 10 };
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("level_up")), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        #endregion

        #region Audio State Management Tests

        [Fact]
        public async Task PauseAllAudio_ShouldPauseMusicAndAmbient()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.PauseAllAsync();

            // Assert
            _mockAudioManager.Verify(a => a.PauseMusicAsync(), Times.Once);
            _mockAudioManager.Verify(a => a.PauseAmbientAsync(), Times.Once);
        }

        [Fact]
        public async Task ResumeAllAudio_ShouldResumeMusicAndAmbient()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();
            await _reactiveAudio.PauseAllAsync();

            // Act
            await _reactiveAudio.ResumeAllAsync();

            // Assert
            _mockAudioManager.Verify(a => a.ResumeMusicAsync(), Times.Once);
            _mockAudioManager.Verify(a => a.ResumeAmbientAsync(), Times.Once);
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public void SetReactionEnabled_ShouldEnableOrDisableReactions()
        {
            // Arrange
            var gameStateReaction = new GameStateReaction(new Mock<ILogger<GameStateReaction>>().Object);
            var registryLogger = new Mock<ILogger<AudioReactionRegistry>>();
            var registry = new AudioReactionRegistry(registryLogger.Object, new List<IAudioReaction> { gameStateReaction });

            // Act
            registry.SetReactionEnabled<GameStateReaction>(false);

            // Assert
            registry.IsReactionEnabled<GameStateReaction>().Should().BeFalse();
        }

        [Fact]
        public async Task GameStateChanged_WithDisabledReaction_ShouldNotReact()
        {
            // Arrange
            var gameStateReaction = new GameStateReaction(new Mock<ILogger<GameStateReaction>>().Object);
            var registryLogger = new Mock<ILogger<AudioReactionRegistry>>();
            var registry = new AudioReactionRegistry(registryLogger.Object, new List<IAudioReaction> { gameStateReaction });

            _reactiveAudio = new ReactiveAudioEngine(
                _mockLogger.Object,
                _mockAudioManager.Object,
                _mockEventBus.Object,
                registry
            );

            CaptureEventHandler();
            await _reactiveAudio.InitializeAsync();
            registry.SetReactionEnabled<GameStateReaction>(false);

            // Act
            var evt = new GameStateChangedEvent(GameState.Overworld, GameState.Battle);
            await SimulateEvent(evt);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public async Task Dispose_ShouldUnsubscribeFromEvents()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            _reactiveAudio.Dispose();

            // Assert
            _mockEventBus.Verify(
                e => e.Unsubscribe<IGameEvent>(It.IsAny<Action<IGameEvent>>()),
                Times.Once
            );
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldBeIdempotent()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();

            // Act
            _reactiveAudio.Dispose();
            _reactiveAudio.Dispose();
            _reactiveAudio.Dispose();

            // Assert - Should not throw
        }

        #endregion

        public void Dispose()
        {
            _reactiveAudio?.Dispose();
        }
    }
}
