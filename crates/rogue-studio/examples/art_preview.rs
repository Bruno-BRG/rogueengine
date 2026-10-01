fn main() {
    let all = rogue_studio::art::all();
    let scale = 8usize;
    let (w, h) = (all.len() * 16 * scale, 16 * scale);
    let mut buf = vec![0u8; w * h * 4];
    for (i, s) in all.iter().enumerate() {
        for y in 0..16 {
            for x in 0..16 {
                let c = s.get(0, x, y).unwrap();
                let c = if c[3] == 0 { [40, 44, 52, 255] } else { c };
                for dy in 0..scale { for dx in 0..scale {
                    let (px, py) = (i * 16 * scale + x as usize * scale + dx, y as usize * scale + dy);
                    buf[(py * w + px) * 4..(py * w + px) * 4 + 4].copy_from_slice(&c);
                }}
            }
        }
    }
    let mut out = Vec::new();
    {
        let mut e = png::Encoder::new(&mut out, w as u32, h as u32);
        e.set_color(png::ColorType::Rgba); e.set_depth(png::BitDepth::Eight);
        e.write_header().unwrap().write_image_data(&buf).unwrap();
    }
    std::fs::write(std::env::args().nth(1).unwrap(), out).unwrap();
}
