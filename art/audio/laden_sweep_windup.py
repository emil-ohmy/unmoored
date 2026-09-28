"""The Laden winds up its sweep (0.6 s): shorter, higher and hissier than the slam's heave, so the two tell apart by
ear: a tight twisting creak climbing fast, a sharp breathy intake, and the heap's junk shifting. Swells to the strike."""
TELEGRAPH = 0.6


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("slam", os.path.join(os.path.dirname(__file__), "laden_slam_windup.py"))
    slam = importlib.util.module_from_spec(spec)
    slam.synth, slam.np = synth, np
    spec.loader.exec_module(slam)
    duration = TELEGRAPH + 0.12
    t = synth.seconds(duration)
    swell = np.clip(t / TELEGRAPH, 0, 1) ** 1.4 * np.clip((duration - t) / 0.12, 0, 1)
    c = slam.creak(duration, rng, 55, 170, [(620, 1.0, 0.008), (1400, 0.7, 0.006), (2700, 0.35, 0.004)], lambda u: 1.0)
    intake = synth.bandpass(synth.white(duration, rng), synth.glide(1500, 2600, duration), 1.6)
    r = slam.rattle(duration, rng, 10, 2500, 5000)
    out = slam.norm(c) * 0.65 + slam.norm(intake) * 0.22 + slam.norm(r) * 0.25
    return out * swell
