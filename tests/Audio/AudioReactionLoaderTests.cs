using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Configuration;
using PokeNET.Domain.ECS.Events;
using Xunit;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Tests for the AudioReactionLoader configuration system.
    /// </summary>
    public class AudioReactionLoaderTests : IDisposable
    {
        private readonly Mock<ILogger<AudioReactionLoader>> _mockLogger;
        private readonly Mock<IAudioManager> _mockAudioManager;
        private readonly string _testConfigPath;
        private AudioReactionLoader? _loader;

        public AudioReactionLoaderTests()
        {
            _mockLogger = new Mock<ILogger<AudioReactionLoader>>();
            _mockAudioManager = new Mock<IAudioManager>();
            _testConfigPath = Path.Combine(Path.GetTempPath(), $"test-audio-reactions-{Guid.NewGuid()}.json");
        }

        public void Dispose()
        {
            _loader?.Dispose();
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }

        [Fact]
        public async Task LoadAsync_ValidConfiguration_LoadsSuccessfully()
        {
            // Arrange
            var json = @"{
                ""reactions"": [
                    {
                        ""name"": ""WildBattleMusic"",
                        ""type"": ""MusicTransition"",
                        ""eventType"": ""BattleStartEvent"",
                        ""enabled"": true,
                        ""conditions"": [
                            {
                                ""property"": ""IsWildBattle"",
                                ""operator"": ""equals"",
                                ""value"": true
                            }
                        ],
                        ""actions"": [
                            {
                                ""type"": ""PlayMusic"",
                                ""path"": ""audio/music/battle_wild.ogg"",
                                ""volume"": 1.0,
                                ""loop"": true
                            }
                        ]
                    }
                ]
            }";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act
            var config = await _loader.LoadAsync();

            // Assert
            Assert.NotNull(config);
            Assert.Single(config.Reactions);
            Assert.Equal("WildBattleMusic", config.Reactions[0].Name);
            Assert.Equal("BattleStartEvent", config.Reactions[0].EventType);
            Assert.True(config.Reactions[0].Enabled);
        }

        [Fact]
        public async Task LoadAsync_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.json");
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, nonExistentPath);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _loader.LoadAsync());
        }

        [Fact]
        public async Task LoadAsync_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            await File.WriteAllTextAsync(_testConfigPath, "{ invalid json }");
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act & Assert
            await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => _loader.LoadAsync());
        }

        [Fact]
        public async Task Validation_MissingReactionName_ThrowsInvalidOperationException()
        {
            // Arrange
            var json = @"{
                ""reactions"": [
                    {
                        ""name"": """",
                        ""type"": ""MusicTransition"",
                        ""eventType"": ""BattleStartEvent"",
                        ""enabled"": true,
                        ""conditions"": [],
                        ""actions"": [
                            {
                                ""type"": ""PlayMusic"",
                                ""path"": ""audio/music/battle.ogg""
                            }
                        ]
                    }
                ]
            }";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync());
        }

        [Fact]
        public async Task Validation_InvalidOperator_ThrowsInvalidOperationException()
        {
            // Arrange
            var json = @"{
                ""reactions"": [
                    {
                        ""name"": ""TestReaction"",
                        ""type"": ""MusicTransition"",
                        ""eventType"": ""BattleStartEvent"",
                        ""enabled"": true,
                        ""conditions"": [
                            {
                                ""property"": ""IsWildBattle"",
                                ""operator"": ""invalidOperator"",
                                ""value"": true
                            }
                        ],
                        ""actions"": [
                            {
                                ""type"": ""PlayMusic"",
                                ""path"": ""audio/music/battle.ogg""
                            }
                        ]
                    }
                ]
            }";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync());
        }

        [Fact]
        public async Task Validation_InvalidActionType_ThrowsInvalidOperationException()
        {
            // Arrange
            var json = @"{
                ""reactions"": [
                    {
                        ""name"": ""TestReaction"",
                        ""type"": ""MusicTransition"",
                        ""eventType"": ""BattleStartEvent"",
                        ""enabled"": true,
                        ""conditions"": [],
                        ""actions"": [
                            {
                                ""type"": ""InvalidAction"",
                                ""path"": ""audio/music/battle.ogg""
                            }
                        ]
                    }
                ]
            }";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync());
        }

        [Fact]
        public async Task Validation_VolumeOutOfRange_ThrowsInvalidOperationException()
        {
            // Arrange
            var json = @"{
                ""reactions"": [
                    {
                        ""name"": ""TestReaction"",
                        ""type"": ""MusicTransition"",
                        ""eventType"": ""BattleStartEvent"",
                        ""enabled"": true,
                        ""conditions"": [],
                        ""actions"": [
                            {
                                ""type"": ""PlayMusic"",
                                ""path"": ""audio/music/battle.ogg"",
                                ""volume"": 2.5
                            }
                        ]
                    }
                ]
            }";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync());
        }

        [Fact]
        public void EvaluateCondition_EqualsOperator_ReturnsTrueForMatch()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            File.WriteAllText(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var condition = new ConditionDefinition
            {
                Property = "IsWildBattle",
                Operator = "equals",
                Value = true
            };
            var evt = new BattleStartEvent { IsWildBattle = true };

            // Act
            var result = _loader.EvaluateCondition(condition, evt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EvaluateCondition_NotEqualsOperator_ReturnsTrueForNonMatch()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            File.WriteAllText(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var condition = new ConditionDefinition
            {
                Property = "IsWildBattle",
                Operator = "notEquals",
                Value = false
            };
            var evt = new BattleStartEvent { IsWildBattle = true };

            // Act
            var result = _loader.EvaluateCondition(condition, evt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EvaluateCondition_LessThanOperator_ReturnsTrueWhenLess()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            File.WriteAllText(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var condition = new ConditionDefinition
            {
                Property = "HealthPercentage",
                Operator = "lessThan",
                Value = 0.5
            };
            var evt = new HealthChangedEvent { HealthPercentage = 0.25f };

            // Act
            var result = _loader.EvaluateCondition(condition, evt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EvaluateCondition_GreaterThanOperator_ReturnsTrueWhenGreater()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            File.WriteAllText(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var condition = new ConditionDefinition
            {
                Property = "HealthPercentage",
                Operator = "greaterThan",
                Value = 0.5
            };
            var evt = new HealthChangedEvent { HealthPercentage = 0.75f };

            // Act
            var result = _loader.EvaluateCondition(condition, evt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EvaluateCondition_ContainsOperator_ReturnsTrueWhenContains()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            File.WriteAllText(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var condition = new ConditionDefinition
            {
                Property = "PokemonName",
                Operator = "contains",
                Value = "Pika"
            };
            var evt = new PokemonFaintEvent { PokemonName = "Pikachu" };

            // Act
            var result = _loader.EvaluateCondition(condition, evt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteActionAsync_PlayMusic_CallsAudioManager()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var action = new ActionDefinition
            {
                Type = "PlayMusic",
                Path = "audio/music/test.ogg",
                Volume = 0.8f
            };

            // Act
            await _loader.ExecuteActionAsync(action);

            // Assert
            _mockAudioManager.Verify(m => m.PlayMusicAsync("audio/music/test.ogg", 0.8f, default), Times.Once);
        }

        [Fact]
        public async Task ExecuteActionAsync_PlaySound_CallsAudioManager()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var action = new ActionDefinition
            {
                Type = "PlaySound",
                Path = "audio/sfx/test.wav",
                Volume = 1.0f
            };

            // Act
            await _loader.ExecuteActionAsync(action);

            // Assert
            _mockAudioManager.Verify(m => m.PlaySoundEffectAsync("audio/sfx/test.wav", 1.0f, default), Times.Once);
        }

        [Fact]
        public async Task ExecuteActionAsync_StopAll_CallsAudioManager()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            var action = new ActionDefinition
            {
                Type = "StopAll"
            };

            // Act
            await _loader.ExecuteActionAsync(action);

            // Assert
            _mockAudioManager.Verify(m => m.StopAll(), Times.Once);
        }

        [Fact]
        public void EnableHotReload_ValidPath_StartsFileWatcher()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            File.WriteAllText(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act
            _loader.EnableHotReload();

            // Assert - No exception should be thrown
            Assert.NotNull(_loader);
        }

        [Fact]
        public async Task HotReload_FileChanged_RaisesConfigurationReloadedEvent()
        {
            // Arrange
            var json = @"{""reactions"":[]}";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);
            await _loader.LoadAsync();
            _loader.EnableHotReload();

            var eventRaised = false;
            _loader.ConfigurationReloaded += (sender, e) => eventRaised = true;

            // Act
            await Task.Delay(100); // Give file watcher time to initialize
            await File.WriteAllTextAsync(_testConfigPath, json);
            await Task.Delay(500); // Give file watcher time to detect change

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public async Task LoadAsync_MultipleConditions_LoadsAllConditions()
        {
            // Arrange
            var json = @"{
                ""reactions"": [
                    {
                        ""name"": ""ComplexReaction"",
                        ""type"": ""MusicTransition"",
                        ""eventType"": ""BattleStartEvent"",
                        ""enabled"": true,
                        ""conditions"": [
                            {
                                ""property"": ""IsWildBattle"",
                                ""operator"": ""equals"",
                                ""value"": false
                            },
                            {
                                ""property"": ""IsGymLeader"",
                                ""operator"": ""equals"",
                                ""value"": true
                            }
                        ],
                        ""actions"": [
                            {
                                ""type"": ""PlayMusic"",
                                ""path"": ""audio/music/gym.ogg""
                            }
                        ]
                    }
                ]
            }";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act
            var config = await _loader.LoadAsync();

            // Assert
            Assert.Equal(2, config.Reactions[0].Conditions.Count);
            Assert.Equal("IsWildBattle", config.Reactions[0].Conditions[0].Property);
            Assert.Equal("IsGymLeader", config.Reactions[0].Conditions[1].Property);
        }

        [Fact]
        public async Task LoadAsync_MultipleActions_LoadsAllActions()
        {
            // Arrange
            var json = @"{
                ""reactions"": [
                    {
                        ""name"": ""ComplexReaction"",
                        ""type"": ""MusicTransition"",
                        ""eventType"": ""BattleStartEvent"",
                        ""enabled"": true,
                        ""conditions"": [],
                        ""actions"": [
                            {
                                ""type"": ""FadeOut"",
                                ""channel"": ""Music"",
                                ""duration"": 0.5
                            },
                            {
                                ""type"": ""PlayMusic"",
                                ""path"": ""audio/music/battle.ogg"",
                                ""volume"": 1.0
                            }
                        ]
                    }
                ]
            }";
            await File.WriteAllTextAsync(_testConfigPath, json);
            _loader = new AudioReactionLoader(_mockLogger.Object, _mockAudioManager.Object, _testConfigPath);

            // Act
            var config = await _loader.LoadAsync();

            // Assert
            Assert.Equal(2, config.Reactions[0].Actions.Count);
            Assert.Equal("FadeOut", config.Reactions[0].Actions[0].Type);
            Assert.Equal("PlayMusic", config.Reactions[0].Actions[1].Type);
        }
    }
}
