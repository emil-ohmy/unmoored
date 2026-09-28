"""The Laden heaves its heap up for the slam (0.9 s, the length of the wind-up): a deep wooden creak whose
stick-slip rate climbs as the load rises, the junk in the heap rattling more and more, and a low effortful groan,
all swelling to a peak right as the strike starts. The tell: the sound itself says when the blow is coming."""
TELEGRAPH = 0.9


def creak(duration, rng, rate_from, rate_to, resonances, gain):
    """Stick-slip friction: a train of tiny impulses at a rising rate, rung through a few wooden resonances."""
    out = np.zeros(int(duration * synth.SR))
    t = 0.0
    while t < duration:
        u = t / duration
        rate = rate_from + (rate_to - rate_from) * u ** 1.3
        i = int(t * synth.SR)
        out[i] += (0.3 + 0.7 * u) * rng.uniform(0.6, 1.0) * gain(u)
        t += (1.0 / rate) * rng.uniform(0.8, 1.2)
    n = int(0.04 * synth.SR)
    tt = np.arange(n) / synth.SR
    ir = sum(a * np.sin(2 * np.pi * f * tt) * np.exp(-tt / tau) for f, a, tau in resonances)
    return synth.fft_convolve(out, ir)[:len(out)]


def rattle(duration, rng, count, low, high):
    out = np.zeros(int(duration * synth.SR))
    for _ in range(count):
        at = duration * rng.uniform(0, 1) ** 0.5  # denser towards the end
        d = rng.uniform(0.004, 0.012)
        click = synth.bandpass(synth.white(d, rng), rng.uniform(low, high), 3.0) * synth.decay(d, d / 3)
        synth.place(out, click * rng.uniform(0.3, 1.0), at)
    return out


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def render(v, rng):
    duration = TELEGRAPH + 0.15
    t = synth.seconds(duration)
    swell = np.clip(t / TELEGRAPH, 0, 1) ** 1.6 * np.clip((duration - t) / 0.15, 0, 1)
    c = creak(duration, rng, 22, 75, [(330, 1.0, 0.012), (780, 0.6, 0.008), (1600, 0.3, 0.005)], lambda u: 1.0)
    groan = synth.voice(synth.glide(82, 104, duration, 1.2), duration, rng, "oh", vibrato=0.02, max_freq=1400)
    groan = synth.lowpass(norm(groan), 900)
    r = rattle(duration, rng, 16, 1800, 4200)
    out = norm(c) * 0.75 + norm(groan) * 0.45 + norm(r) * 0.3
    return out * swell
