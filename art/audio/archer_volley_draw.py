"""The Laden archer readies a volley (1.3 s, its longer wind-up): three wooden clicks as three arrows are nocked, a
rattle from the quiver, then a slower, deeper draw than a single shot, swelling to the first release."""
TELEGRAPH = 1.3


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("slam", os.path.join(os.path.dirname(__file__), "laden_slam_windup.py"))
    slam = importlib.util.module_from_spec(spec)
    slam.synth, slam.np = synth, np
    spec.loader.exec_module(slam)
    duration = TELEGRAPH + 0.08
    out = np.zeros(int(duration * synth.SR))
    for at in (0.05, 0.22, 0.39):  # nock, nock, nock
        d = 0.03
        click = synth.bandpass(synth.white(d, rng), rng.uniform(1300, 1700), 5.0) * synth.decay(d, 0.006)
        synth.place(out, slam.norm(click) * 0.7, at)
    synth.place(out, slam.norm(slam.rattle(0.5, rng, 9, 2500, 5000)) * 0.25, 0.0)
    draw_len = duration - 0.45
    t = synth.seconds(draw_len)
    swell = np.clip(t / (TELEGRAPH - 0.45), 0, 1) ** 1.4 * np.clip((draw_len - t) / 0.08, 0, 1)
    creak = slam.creak(draw_len, rng, 30, 110, [(700, 1.0, 0.007), (1500, 0.6, 0.005), (2600, 0.3, 0.003)], lambda u: 1.0)
    hum = synth.sine(synth.glide(140, 280, draw_len, 1.2), draw_len) + 0.4 * synth.sine(synth.glide(280, 560, draw_len, 1.2), draw_len)
    synth.place(out, (slam.norm(creak) * 0.6 + slam.norm(synth.lowpass(hum, 1300)) * 0.35) * swell, 0.45)
    return out
