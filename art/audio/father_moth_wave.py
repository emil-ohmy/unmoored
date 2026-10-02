"""The channel's water beaten into a wave: hard slaps on the surface, then a long heavy surge of water rolling out
across the stone floor, hissing as it spends itself."""
VARIANTS = 1


def render(v, rng):
    duration = 2.6
    out = np.zeros(int(duration * synth.SR))
    for k in range(3):
        slap = synth.lowpass(synth.white(0.14, rng), 2400) * synth.decay(0.14, 0.03)
        synth.place(out, slap / (np.max(np.abs(slap)) + 1e-9) * (0.5 + k * 0.15), k * 0.13)
    surge = synth.lowpass(synth.brown(2.2, rng), synth.glide(900, 300, 2.2)) * synth.envelope(2.2, 0.25, 0, curve=1.6)
    synth.place(out, surge / (np.max(np.abs(surge)) + 1e-9) * 0.8, 0.35)
    hiss = synth.bandpass(synth.white(2.0, rng), synth.glide(1500, 3000, 2.0), 0.8) * synth.envelope(2.0, 0.6, 0, curve=1.5)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.35, 0.5)
    return out
