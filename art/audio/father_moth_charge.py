"""Father Moth charging his blast (phase 4): twelve seconds that never stop building. A deep drone climbing in pitch,
throbbing faster and faster like a heart about to burst; crackling, denser and denser; at the last a whine screaming up
over it all."""
VARIANTS = 1


def render(v, rng):
    duration = 12.4
    n = int(duration * synth.SR)
    t = synth.seconds(duration)
    u = t / duration
    out = np.zeros(n)
    # The drone: a few harmonics, climbing (slowly, then fast), louder as it goes.
    f = 45.0 * (220.0 / 45.0) ** (u ** 2.2)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    drone = sum(np.sin(k * phase) / k for k in range(1, 6))
    # The throb: a pulse that speeds up from once a second to twelve times.
    rate = 1.0 + 11.0 * u ** 1.8
    throb = 0.55 + 0.45 * np.sin(2 * np.pi * np.cumsum(rate) / synth.SR) ** 2
    drone = drone * throb * (0.25 + 0.75 * u ** 1.3)
    synth.place(out, drone / (np.max(np.abs(drone)) + 1e-9) * 0.6, 0.0)
    # Crackle: short bright ticks, denser and denser.
    k = 0.0
    while k < duration:
        tick = synth.bandpass(synth.white(0.02, rng), rng.uniform(2500, 6000), 4.0) * synth.decay(0.02, 0.004)
        synth.place(out, tick / (np.max(np.abs(tick)) + 1e-9) * rng.uniform(0.05, 0.12) * (0.3 + k / duration), k)
        k += rng.uniform(0.02, 0.12) * (1.4 - k / duration)
    # A rushing wind, inward, building.
    wind = synth.bandpass(synth.white(duration, rng), synth.glide(300, 1800, duration, 1.8), 0.7) * u ** 1.6
    synth.place(out, wind / (np.max(np.abs(wind)) + 1e-9) * 0.35, 0.0)
    # The last three seconds: a whine screaming up.
    whine_t = 3.0
    whine = synth.sine(synth.glide(600, 2400, whine_t, 2.0), whine_t) * np.linspace(0, 1, int(whine_t * synth.SR)) ** 2
    synth.place(out, whine * 0.18, duration - whine_t - 0.1)
    return out
