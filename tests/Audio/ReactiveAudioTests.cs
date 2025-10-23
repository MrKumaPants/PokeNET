using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio;
using PokeNET.Audio.Reactive;
using PokeNET.Domain.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Comprehensive tests for ReactiveAudioEngine
    /// Tests state-based audio reactions and event handling
    /// </summary>
    public class ReactiveAudioTests : IDisposable
    {
        private readonly Mock<ILogger<ReactiveAudioEngine>> _mockLogger;
        private readonly Mock<IAudioManager> _mockAudioManager;
        private readonly Mock<IEventBus> _mockEventBus;
        private ReactiveAudioEngine _reactiveAudio;

        public ReactiveAudioTests()
        {
            _mockLogger = new Mock<ILogger<ReactiveAudioEngine>>();
            _mockAudioManager = new Mock<IAudioManager>();
            _mockEventBus = new Mock<IEventBus>();
        }

        private ReactiveAudioEngine CreateReactiveAudio()
        {
            return new ReactiveAudioEngine(
                _mockLogger.Object,
                _mockAudioManager.Object,
                _mockEventBus.Object
            );
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
            _mockEventBus.Verify(e => e.Subscribe<GameStateChangedEvent>(It.IsAny<Action<GameStateChangedEvent>>()), Times.Once);
            _mockEventBus.Verify(e => e.Subscribe<BattleEvent>(It.IsAny<Action<BattleEvent>>()), Times.Once);
        }

        #endregion

        #region Game State Reaction Tests

        [Fact]
        public async Task OnGameStateChanged_ToBattle_ShouldPlayBattleMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnGameStateChangedAsync(GameState.Overworld, GameState.Battle);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("battle")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnGameStateChanged_ToOverworld_ShouldPlayOverworldMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnGameStateChangedAsync(GameState.Battle, GameState.Overworld);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("overworld") || s.Contains("route")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnGameStateChanged_ToMenu_ShouldDuckMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnGameStateChangedAsync(GameState.Overworld, GameState.Menu);

            // Assert
            _mockAudioManager.Verify(
                a => a.DuckMusicAsync(It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnGameStateChanged_FromMenu_ShouldUnduckMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();
            await _reactiveAudio.OnGameStateChangedAsync(GameState.Overworld, GameState.Menu);

            // Act
            await _reactiveAudio.OnGameStateChangedAsync(GameState.Menu, GameState.Overworld);

            // Assert
            _mockAudioManager.Verify(
                a => a.StopDuckingAsync(),
                Times.Once
            );
        }

        [Theory]
        [InlineData(GameState.Battle)]
        [InlineData(GameState.Overworld)]
        [InlineData(GameState.Menu)]
        [InlineData(GameState.Cutscene)]
        public async Task OnGameStateChanged_ToAnyState_ShouldTransitionMusic(GameState targetState)
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnGameStateChangedAsync(GameState.Overworld, targetState);

            // Assert
            _mockLogger.VerifyLog(LogLevel.Information, Times.AtLeastOnce());
        }

        #endregion

        #region Battle Event Reaction Tests

        [Fact]
        public async Task OnBattleStart_ShouldPlayBattleIntro()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnBattleStartAsync(new BattleStartEvent { IsWildBattle = true });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("battle_start")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnBattleStart_WildBattle_ShouldPlayWildBattleMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnBattleStartAsync(new BattleStartEvent { IsWildBattle = true });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("wild")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnBattleStart_TrainerBattle_ShouldPlayTrainerBattleMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnBattleStartAsync(new BattleStartEvent { IsWildBattle = false, IsGymLeader = false });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("trainer")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnBattleStart_GymLeaderBattle_ShouldPlayGymLeaderMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnBattleStartAsync(new BattleStartEvent { IsWildBattle = false, IsGymLeader = true });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("gym") || s.Contains("leader")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnBattleWin_ShouldPlayVictoryMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnBattleEndAsync(new BattleEndEvent { PlayerWon = true });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("victory")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnPokemonFaint_ShouldPlayFaintSound()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnPokemonFaintAsync(new PokemonFaintEvent { PokemonName = "Pikachu" });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("faint")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnAttackUse_ShouldPlayAttackSound()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnAttackUseAsync(new AttackEvent { AttackName = "Thunderbolt", AttackType = "Electric" });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.IsAny<string>(), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnCriticalHit_ShouldPlayCritSound()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnCriticalHitAsync(new CriticalHitEvent());

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("critical")), It.IsAny<float>()),
                Times.Once
            );
        }

        #endregion

        #region Health-Based Reactions Tests

        [Fact]
        public async Task OnHealthChanged_BelowLowThreshold_ShouldPlayLowHealthMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnHealthChangedAsync(new HealthChangedEvent
            {
                CurrentHealth = 15,
                MaxHealth = 100,
                HealthPercentage = 0.15f
            });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.Is<string>(s => s.Contains("low_health")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnHealthChanged_AboveLowThreshold_ShouldStopLowHealthMusic()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();
            await _reactiveAudio.OnHealthChangedAsync(new HealthChangedEvent { HealthPercentage = 0.15f });

            // Act
            await _reactiveAudio.OnHealthChangedAsync(new HealthChangedEvent { HealthPercentage = 0.5f });

            // Assert
            _mockAudioManager.Verify(
                a => a.StopMusicAsync(),
                Times.AtLeastOnce
            );
        }

        #endregion

        #region Weather and Environment Tests

        [Fact]
        public async Task OnWeatherChange_ToRain_ShouldPlayRainAmbient()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnWeatherChangedAsync(new WeatherChangedEvent { NewWeather = Weather.Rain });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayAmbientAsync(It.Is<string>(s => s.Contains("rain")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnWeatherChange_ToClear_ShouldStopAmbient()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();
            await _reactiveAudio.OnWeatherChangedAsync(new WeatherChangedEvent { NewWeather = Weather.Rain });

            // Act
            await _reactiveAudio.OnWeatherChangedAsync(new WeatherChangedEvent { NewWeather = Weather.Clear });

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
        public async Task OnWeatherChange_ToWeatherType_ShouldPlayCorrespondingAmbient(Weather weather)
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnWeatherChangedAsync(new WeatherChangedEvent { NewWeather = weather });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayAmbientAsync(It.IsAny<string>(), It.IsAny<float>()),
                Times.Once
            );
        }

        #endregion

        #region Item and Interaction Tests

        [Fact]
        public async Task OnItemUse_ShouldPlayItemSound()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnItemUseAsync(new ItemUseEvent { ItemName = "Potion" });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.IsAny<string>(), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnPokemonCaught_ShouldPlayCatchSound()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnPokemonCaughtAsync(new PokemonCaughtEvent { PokemonName = "Bulbasaur" });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("catch")), It.IsAny<float>()),
                Times.Once
            );
        }

        [Fact]
        public async Task OnLevelUp_ShouldPlayLevelUpSound()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();

            // Act
            await _reactiveAudio.OnLevelUpAsync(new LevelUpEvent { NewLevel = 10 });

            // Assert
            _mockAudioManager.Verify(
                a => a.PlaySoundEffectAsync(It.Is<string>(s => s.Contains("level_up")), It.IsAny<float>()),
                Times.Once
            );
        }

        #endregion

        #region Audio State Management Tests

        [Fact]
        public void GetCurrentMusicState_ShouldReturnCorrectState()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            _mockAudioManager.Setup(a => a.IsMusicPlaying).Returns(true);
            _mockAudioManager.Setup(a => a.CurrentMusicTrack).Returns("battle_theme");

            // Act
            var state = _reactiveAudio.GetCurrentMusicState();

            // Assert
            state.IsPlaying.Should().BeTrue();
            state.CurrentTrack.Should().Be("battle_theme");
        }

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
            _reactiveAudio = CreateReactiveAudio();

            // Act
            _reactiveAudio.SetReactionEnabled(AudioReactionType.BattleMusic, false);

            // Assert
            _reactiveAudio.IsReactionEnabled(AudioReactionType.BattleMusic).Should().BeFalse();
        }

        [Fact]
        public async Task OnGameStateChanged_WithDisabledReaction_ShouldNotReact()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            await _reactiveAudio.InitializeAsync();
            _reactiveAudio.SetReactionEnabled(AudioReactionType.BattleMusic, false);

            // Act
            await _reactiveAudio.OnGameStateChangedAsync(GameState.Overworld, GameState.Battle);

            // Assert
            _mockAudioManager.Verify(
                a => a.PlayMusicAsync(It.IsAny<string>(), It.IsAny<float>()),
                Times.Never
            );
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldUnsubscribeFromEvents()
        {
            // Arrange
            _reactiveAudio = CreateReactiveAudio();
            _reactiveAudio.InitializeAsync().Wait();

            // Act
            _reactiveAudio.Dispose();

            // Assert
            _mockEventBus.Verify(
                e => e.Unsubscribe<GameStateChangedEvent>(It.IsAny<Action<GameStateChangedEvent>>()),
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

    // Extension for logger verification
    public static class ReactiveAudioLoggerExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel level, Times times)
        {
            mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
    }
}
