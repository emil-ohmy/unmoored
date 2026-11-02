"""The Hold rearing a hand up to slam (the tell), 1.2 s: the great stone hand lifting off the floor and drawn up
high. Stone creaking under the strain (a slow stick-slip of stone joints, quickening and tightening as it climbs:
rougher and higher than the walls' groans, so it reads as effort), a heavy grinding drag under it, the souls in the
wall groaning up with it (a deep many-voiced groan rising), grit and pebbles trickling off the raised hand. Swells
to the top and stops there: the slam follows. Variants close kin on purpose (a tell): 1 plain, 2 the creak higher
and tighter, 3 the groan deeper. Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 3
PEAK = 0.85

SR = synth.SR


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def creak(k, d, tension, rng, pitch=1.0):
    """Stone joints creaking: catches of rough grit whose rate quickens with the tension, rung through hollow
    low-mid resonances that rise a little as it tightens."""
    n = int(d * SR)
    rate = (7 + 55 * tension) * (1 + 0.35 * synth.smooth_random(d, rng, 12.0, -1, 1)[:n])
    catches = np.diff(np.floor(np.cumsum(rate / SR)), prepend=0.0) * rng.uniform(0.4, 1.0, n)
    g = int(0.006 * SR)
    burst = np.hanning(g) * rng.uniform(-1, 1, g)
    x = synth.fft_convolve(catches, burst)[:n]
    centre = 170 * pitch * (1 + 0.5 * tension)
    y = synth.bandpass(x, centre, 3.0) + 0.7 * synth.bandpass(x, centre * 2.03, 3.5) + 0.3 * synth.bandpass(x, centre * 3.9, 3.0)
    return k.norm(y)


def render(v, rng):
    k = kit()
    d = 1.2
    n = int(d * SR)
    t = synth.seconds(d)
    tension = np.clip(t / (d - 0.05), 0, 1) ** 1.4
    out = np.zeros(n)
    out += creak(k, d, tension, rng, [1.0, 1.3, 0.85][v]) * (0.25 + 0.75 * tension) * 0.55
    speed = 0.25 + 0.6 * np.sin(np.clip(t / 0.5, 0, 1) * np.pi / 2) * (1 - 0.3 * tension)
    out += k.norm(k.grind(d, speed, rng, 200)) * 0.45
    f0 = k.curve(d, [(0, [44, 46, 38][v]), (d, [60, 64, 50][v])], 0.05)
    out += k.norm(k.souls(d, rng, f0, 6, "oh", rasp=0.5, breath=0.25, dark=1100)) * tension ** 1.2 * 0.55
    out += k.norm(k.rubble(d, rng, 22, 0.25, 0.9, 0.5)) * 0.18
    # a low push of air as the mass climbs
    flow = tension ** 2
    out += k.norm(k.gust_air(d, flow, rng, inward=True, size=2.2)) * flow * 0.25
    out = synth.highpass(out, 22)
    out *= np.clip(t / 0.12, 0, 1) ** 2  # it eases up off the floor
    out = k.chamber(out, rng, 0.9, 0.25, tail=False)
    return out * np.clip((d - t) / 0.04, 0, 1)
