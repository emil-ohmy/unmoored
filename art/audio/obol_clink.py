"""Obols counted into the merchant's hand: a few small, dull silver clinks tumbling together."""


def clink(freq, rng, duration=0.25):
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for ratio, amp, tau in ((1.0, 1.0, 0.05), (2.76, 0.6, 0.03), (5.4, 0.35, 0.018), (8.9, 0.2, 0.01)):
        out += amp * np.sin(2 * np.pi * freq * ratio * rng.uniform(0.99, 1.01) * t + rng.uniform(0, 6)) * np.exp(-t / tau)
    return out * np.clip(t / 0.0005, 0, 1)


def render(v, rng):
    duration = 0.8
    out = np.zeros(int(duration * synth.SR))
    at = 0.0
    for i in range(6):
        synth.place(out, clink(rng.uniform(2100, 3300), rng) * rng.uniform(0.4, 1.0) * (1 - i * 0.1), at)
        at += rng.uniform(0.04, 0.11)
    return synth.lowpass(out, 7000)
