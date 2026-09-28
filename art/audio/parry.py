"""Perfect block: a bright, ringing metallic 'ting' with a short shimmer and a low thud under it, clearly apart
from the dull clang of a normal block."""
VARIANTS = 2


def render(v, rng):
    duration = 1.1
    k = rng.uniform(0.97, 1.04)
    out = np.zeros(int(duration * synth.SR))
    for freq, tau, amp in ((1760, 0.7, 1.0), (2637, 0.5, 0.6), (3520, 0.35, 0.45), (5274, 0.2, 0.3), (1318, 0.9, 0.35)):
        out += synth.sine(freq * k * rng.uniform(0.995, 1.005), duration, rng.uniform(0, 6)) * synth.decay(duration, tau) * amp
    click = synth.highpass(synth.white(0.01, rng), 3000) * synth.decay(0.01, 0.002)
    thud = synth.sine(synth.glide(140, 70, 0.25), 0.25) * synth.decay(0.25, 0.05) * 0.8
    synth.place(out, click * 0.8, 0.0)
    synth.place(out, thud, 0.0)
    return out * np.clip(synth.seconds(duration) / 0.002, 0, 1)
