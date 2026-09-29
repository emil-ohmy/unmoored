"""Cast and Haul's tell (1.0 s): the anchor hefted back over her shoulder: a long iron creak, the chain gathered up
in her hands with a slow rattle, and a deep breath drawn in, held at the top."""
TELEGRAPH = 1.0


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    import importlib.util as iu, os as _os
    spec = iu.spec_from_file_location("slam", _os.path.join(_os.path.dirname(__file__), "laden_slam_windup.py"))
    slam = iu.module_from_spec(spec)
    slam.synth, slam.np = synth, np
    spec.loader.exec_module(slam)
    duration = TELEGRAPH + 0.12
    t = synth.seconds(duration)
    swell = np.clip(t / TELEGRAPH, 0, 1) ** 1.2 * np.clip((duration - t) / 0.12, 0, 1)
    creak = slam.creak(duration, rng, 12, 45, [(140, 1.0, 0.02), (410, 0.5, 0.012), (980, 0.25, 0.006)], lambda u: 1.0)
    chain = kit.links(duration, rng, 6, 18)
    breath = synth.bandpass(synth.white(duration, rng), synth.glide(500, 1100, duration), 1.2) * np.clip(t / TELEGRAPH, 0, 1) ** 2
    return (kit.norm(creak) * 0.6 + kit.norm(chain) * 0.35 + kit.norm(breath) * 0.3) * swell
