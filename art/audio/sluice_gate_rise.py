"""The sluice gate winding up: an iron chain ratcheting over a pulley, link after link, the heavy board scraping up in
its stone grooves."""
VARIANTS = 1


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    for k in range(int(duration / 0.085)):
        tick = synth.bandpass(synth.white(0.03, rng), rng.uniform(2200, 3200), 3.0) * synth.decay(0.03, 0.006)
        synth.place(out, tick * rng.uniform(0.25, 0.4), k * 0.085 + rng.uniform(0, 0.01))
    scrape = synth.bandpass(synth.white(duration, rng), 500, 1.5) * synth.smooth_random(duration, rng, rate=6.0, low=0.4, high=1.0)
    out += scrape / (np.max(np.abs(scrape)) + 1e-9) * 0.25
    return out * np.clip((duration - synth.seconds(duration)) / 0.2, 0, 1) * np.clip(synth.seconds(duration) / 0.05, 0, 1)
