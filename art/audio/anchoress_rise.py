"""The Anchoress rises into her second phase (2.2 s, before the slam): the anchor lifted to the sky, every link
of the chain shaking, a deep swell under the floor, and her voice climbing from a groan into a drowned, wordless
cry, several voices at once, as if the souls she kept were crying with her."""


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    duration = 2.4
    t = synth.seconds(duration)
    out = np.zeros(len(t))
    swell = np.clip(t / 1.45, 0, 1) ** 1.3 * np.clip((duration - t) / 0.3, 0, 1)
    cry = sum(kit.groan(duration, rng, f, f * r, vowel) * a for f, r, vowel, a in
              ((82, 1.9, "oh", 0.6), (123, 1.7, "ah", 0.45), (165, 1.6, "ah", 0.3), (98, 2.0, "oo", 0.35)))
    chain = kit.links(duration, rng, 30, 120, lambda u: 0.3 + 0.7 * u)
    under = synth.lowpass(synth.brown(duration, rng), 120) + 0.6 * synth.sine(synth.glide(36, 44, duration), duration)
    out += kit.norm(cry) * 0.6 + kit.norm(chain) * 0.35 + kit.norm(under) * 0.5
    hall = synth.reverb_ir(2.0, 1.8, rng, brightness=0.25)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return (out * 0.8 + kit.norm(wet) * 0.35) * swell
