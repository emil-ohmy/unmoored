"""One of the Kept beaten, melting back down into the stone, 1.6-2 s: a sigh going down (a thin, rasping voice falling
nearly an octave, then only breath), muffled more and more as it goes into the rock; a wet, draining gurgle
under it (thick bubbles, lower and fewer as it drains, a sucking hush pulsing like something poured away); then
the stone closing over it (hold_stone_press's soft clay grind, low, and a dull settle). Pitiful, not a monster's
death: something worn down to nothing let go of.
Variants: 1 plain, 2 shorter, a little higher, 3 the longest and deepest.
Uses kept_emerge's code. `synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 3
PEAK = 0.75

SR = synth.SR
# (length, sigh from Hz, voiced, sigh length, stone colour Hz)
SHAPES = [(1.8, 440, 0.5, 1.05, 420), (1.6, 490, 0.5, 0.9, 480), (2.0, 400, 0.55, 1.2, 360)]


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kept", os.path.join(os.path.dirname(__file__), "kept_emerge.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)

    d, f0, voiced, sd, colour = SHAPES[v]
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    # the sigh, going down and into the rock
    st = synth.seconds(sd)
    sigh = kit.throat(sd, rng, synth.glide(f0, f0 * 0.52, sd, 0.8), synth.glide(900, 380, sd), voiced, 0.4, "oh")
    voice_fade = np.clip(1 - st / (sd * 0.75), 0, 1)  # the voice goes first, breath stays
    breath = synth.bandpass(synth.white(sd, rng), synth.glide(1100, 350, sd), 1.4)
    sigh = kit.norm(sigh) * (0.25 + 0.75 * voice_fade) + kit.norm(breath) * 0.35
    sigh = synth.lowpass(sigh, synth.glide(4000, 380, sd, 0.6))
    sigh *= np.clip(st / 0.06, 0, 1) * np.clip(1 - st / sd, 0, 1) ** 1.3
    synth.place(out, kit.norm(sigh) * 0.75, 0.0)
    # draining: thick bubbles, lower and fewer; a sucking hush pulsing
    gd = d - 0.5
    gurgle = kit.gurgle(gd, rng, 30, 150, 520, density=lambda u: (1 - u) ** 1.3 * np.clip(u / 0.15, 0, 1),
                        pitch=lambda u: 1 - 0.5 * u)
    synth.place(out, kit.norm(gurgle) * 0.5, 0.2)
    dd = d - 0.6
    dt = synth.seconds(dd)
    suck = synth.bandpass(synth.white(dd, rng), synth.glide(750, 200, dd), 2.0)
    suck *= synth.smooth_random(dd, rng, 11.0, 0.0, 1.0)[:len(suck)] ** 2 * np.sin(np.clip(dt / dd, 0, 1) * np.pi) ** 1.2
    synth.place(out, kit.norm(synth.lowpass(suck, 1200)) * 0.35, 0.3)
    # the stone closing over it
    stone = kit.load("hold_stone_press")
    gs = d * 0.36  # (it begins as the sigh ends: no gap between)
    speed = kit.hump(d, gs, d - gs - 0.1, 1.4) * 0.75
    synth.place(out, kit.norm(stone.clay(d, speed, colour, rng)) * 0.4, 0.0)
    settle = kit.thump(rng, 65, 0.25)
    synth.place(out, kit.norm(synth.lowpass(settle, 300)) * 0.28, d - 0.32)
    out = synth.highpass(out, 35)
    out = kit.room(out, rng, 0.18)
    return out * np.clip((d - t) / 0.12, 0, 1) ** 2
