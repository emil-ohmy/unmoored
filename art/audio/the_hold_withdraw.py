"""A wounded hand of the Hold sinking back into the wall, 1.0 s: a soft grinding swallow. Stone giving like clay as
the arm slides in (hold_stone_press's slow granular drag, low), the wall taking it with a deep gulp (a low tone
bending down under a sucking hush that falls), a wet slither as the stone closes over, a dull soft settle.
Variants: 1 plain, 2 slower and deeper. Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 2
PEAK = 0.55  # soft: a swallow, not a blow

SR = synth.SR


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def render(v, rng):
    k = kit()
    stone = k.load("hold_stone_press")
    d = [1.0, 1.15][v]
    n = int(d * SR)
    t = synth.seconds(d)
    slow = 1 + 0.15 * v
    speed = k.curve(d, [(0, 0.2), (0.12, 0.85), (0.55 * slow, 0.6), (0.85 * slow, 0.05), (d, 0.0)], 0.04)
    out = k.norm(stone.clay(d, speed, 300 - 40 * v, rng)) * 0.6
    out += k.norm(k.grind(d, speed * 0.6, rng, 160)) * 0.35
    # the gulp: a low tone bending down, under a sucking hush that falls
    gulp = synth.sine(synth.glide(110 - 15 * v, 42, d, 0.6), d) * np.sin(np.clip((t - 0.15) / (0.6 * slow), 0, 1) * np.pi) ** 2
    suck = synth.bandpass(synth.white(d, rng), synth.glide(900, 220, d, 0.7), 1.3) * np.sin(np.clip((t - 0.1) / (0.7 * slow), 0, 1) * np.pi) ** 1.5
    out += k.norm(gulp) * 0.5 + k.norm(suck) * 0.25
    out += k.norm(stone.slither(d, rng, 0.62 * slow, 0.3)) * 0.18
    settle = k.impact(rng, 55, 0.3, 0.35, 0.15)
    synth.place(out, k.norm(settle) * 0.3, min(d - 0.3, 0.82 * slow))
    out = synth.highpass(out, 25)
    out *= np.clip(t / 0.03, 0, 1)
    out = k.chamber(out, rng, 0.9, 0.2)
    return k.trim(out, -48, 0.2)
