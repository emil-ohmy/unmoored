"""The Hold letting a breath go: the cave's walls breathing out, 4.5 s. The same air, cracks and stone as
hold_breath_in (whose code it uses), turned round: the flow swells soon (about 30% in) from nothing and sinks away
slowly, lower and softer than the draw; the cracks' whistles drift down, the stone strains a little more as the
walls settle. Starts from nothing (it follows an in), ends in nothing (an in follows it). Mono, quiet.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 2
PEAK = 0.5  # quiet: the game plays these under everything


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("breath", os.path.join(os.path.dirname(__file__), "hold_breath_in.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    return kit.breath("out", v, rng)
