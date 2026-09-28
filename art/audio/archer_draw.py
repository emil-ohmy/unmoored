"""The Laden archer draws (0.9 s, its wind-up): the bow's taut creak tightening and a thin string hum rising in pitch,
swelling to the release. Higher and thinner than the Laden's heave, so the two tell apart by ear."""
TELEGRAPH = 0.9


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("slam", os.path.join(os.path.dirname(__file__), "laden_slam_windup.py"))
    slam = importlib.util.module_from_spec(spec)
    slam.synth, slam.np = synth, np
    spec.loader.exec_module(slam)
    duration = TELEGRAPH + 0.08
    t = synth.seconds(duration)
    swell = np.clip(t / TELEGRAPH, 0, 1) ** 1.3 * np.clip((duration - t) / 0.08, 0, 1)
    creak = slam.creak(duration, rng, 40, 140, [(900, 1.0, 0.006), (1900, 0.6, 0.004), (3100, 0.3, 0.003)], lambda u: 1.0)
    hum = synth.sine(synth.glide(180, 330, duration, 1.2), duration) + 0.4 * synth.sine(synth.glide(360, 660, duration, 1.2), duration)
    hum = synth.lowpass(hum, 1500)
    return (slam.norm(creak) * 0.6 + slam.norm(hum) * 0.3) * swell
