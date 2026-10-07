"""The ratchet catching the sluice gate: a heavy iron pawl dropping into its tooth, a deep wooden knock under it."""
VARIANTS = 1


def render(v, rng):
    duration = 0.8
    out = np.zeros(int(duration * synth.SR))
    clank = synth.bandpass(synth.white(0.25, rng), 1400, 4.0) * synth.decay(0.25, 0.05)
    synth.place(out, clank / (np.max(np.abs(clank)) + 1e-9) * 0.6, 0.0)
    knock = synth.lowpass(synth.brown(0.4, rng), 180) * synth.decay(0.4, 0.08)
    synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.7, 0.0)
    ring = synth.sine(820, 0.6) * synth.decay(0.6, 0.12) + 0.5 * synth.sine(1370, 0.6) * synth.decay(0.6, 0.08)
    ring *= np.clip((0.6 - synth.seconds(0.6)) / 0.2, 0, 1)  # (faded out, not cut)
    synth.place(out, ring * 0.12, 0.0)
    return out * np.clip((duration - synth.seconds(duration)) / 0.15, 0, 1)
