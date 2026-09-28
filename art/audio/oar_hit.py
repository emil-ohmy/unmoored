"""The oar lands on a Soured: a dull body thud, a cloth-and-flesh smack, the wooden knock of the oar, and a short
hollow ring of the soul inside. Distinct from the block clang (no metal). Heavy hits play it lower (Sfx)."""
VARIANTS = 3


def render(v, rng):
    duration = 0.5
    k = rng.uniform(0.92, 1.08)
    out = np.zeros(int(duration * synth.SR))
    thud = synth.sine(synth.glide(125 * k, 52 * k, 0.2, 0.6), 0.2) * synth.decay(0.2, 0.05)
    smack = synth.lowpass(synth.white(0.08, rng), 1900) * synth.decay(0.08, 0.018)
    smack += 0.6 * synth.bandpass(synth.white(0.08, rng), 520 * k, 1.5) * synth.decay(0.08, 0.03)
    knock = synth.bandpass(synth.white(0.04, rng), 1150 * k, 4.0) * synth.decay(0.04, 0.012)
    hollow = synth.bandpass(synth.white(duration, rng), 340 * k, 7.0) * synth.decay(duration, 0.12)
    ring = synth.sine(synth.glide(620 * k, 300 * k, duration, 0.5), duration, rng.uniform(0, 6)) * synth.decay(duration, 0.14)
    synth.place(out, thud * 1.0, 0.0)
    synth.place(out, smack / (np.max(np.abs(smack)) + 1e-9) * 0.7, 0.0)
    synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.35, 0.0)
    synth.place(out, hollow / (np.max(np.abs(hollow)) + 1e-9) * 0.35, 0.01)
    synth.place(out, ring * 0.12, 0.015)
    return out * np.clip(synth.seconds(duration) / 0.001, 0, 1)
