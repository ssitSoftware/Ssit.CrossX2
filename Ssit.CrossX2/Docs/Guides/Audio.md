# Audio

CrossX2 audio provides sound effects, reusable common sounds, music playlists, and spatial sound parameters.

## Sound Manager

`ISoundManager` owns global audio volume state.

```csharp
soundManager.SoundVolume = 0.8f;
soundManager.MusicVolume = 0.5f;
```

It also exposes `SoundVolumeUpdated` and `Disposing` events.

## Sound Effects

`ISoundEffect` represents loaded sound data. It can play once or create instances for controlled playback.

```csharp
using var handle = contentManager.Get<ISoundEffect>("assets:/Sfx/Hit.wav");
var effect = handle.Resource;

effect.PlayOnce();
```

For controlled playback:

```csharp
var instance = effect.CreateInstance();
instance.Parameters = new SoundParameters(Volume: 1f, Pitch: 1f, Pan: 0f);
instance.Play();
```

`ISoundEffectInstance` supports `Play`, `Stop`, `Pause`, `Resume`, looping, and the `Finished` event.

## Spatial Parameters

`SoundParameters` stores volume, pitch, and pan. It can apply an `ISoundEmitter` and `ISoundListener` to produce distance attenuation and pan.

```csharp
instance.Emitter = emitter;
instance.Listener = listener;
instance.Parameters = new SoundParameters(1f, 1f);
```

The listener provides position, max distance, and pan distance.

## Common Sounds

`ICommonSoundContainer` registers named sound effects and reuses instances while playing them.

Register the container:

```csharp
builder.WithCommonSoundsContainer();
```

Register and play sounds:

```csharp
commonSounds
    .RegisterSound("Select", "assets:/Sfx/Select.wav")
    .RegisterSound("Back", "assets:/Sfx/Back.wav", volume: 0.8f);

commonSounds.Play("Select");
```

Use this for short UI or gameplay sounds that are triggered often.

## Music Playlists

`IMusicPlayer` controls named playlists:

```csharp
musicPlayer
    .RegisterPlaylist("Menu", "assets:/Music/Menu.ogg")
    .RegisterPlaylist("Game", "assets:/Music/Game.ogg");

musicPlayer.ChangePlaylist("Menu", fadeTimeMs: 250);
```

Playlists can be reset and navigated:

```csharp
musicPlayer.ResetPlaylistPosition("Game");
musicPlayer.NextTrack(fadeTimeMs: 500);
musicPlayer.PreviousTrack(fadeTimeMs: 500);
```

`Song` can be created from a path or a `(name, path)` tuple.
