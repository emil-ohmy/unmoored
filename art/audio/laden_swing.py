"""The heap swung through the air: a heavy, low whoosh with a rumble under it. Plays at the start of every Laden
strike, hit or miss (the sweep plays it a little higher). Hearing it pass is how a dodge sounds."""
VARIANTS = 3


def render(v, rng):
    duration = 0.5
    k = rng.uniform(0.9, 1.1)
    air = synth.bandpass(synth.white(duration, rng), synth.arc(170 * k, 750 * k, duration, 0.4), 1.1)
    air = synth.lowpass(synth.lowpass(air, 1600), 1600)  # heavy, not hissy
    air *= synth.envelope(duration, 0, 0, curve=2.5, peak_at=0.2)
    rumble = synth.lowpass(synth.brown(duration, rng), 140) * synth.envelope(duration, 0, 0, curve=2.0, peak_at=0.18)
    return air / (np.max(np.abs(air)) + 1e-9) + rumble / (np.max(np.abs(rumble)) + 1e-9) * 0.5
