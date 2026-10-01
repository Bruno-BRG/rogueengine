//! Starter pixel art for the template project, drawn as character maps.
//! `.` is transparent; each sprite brings its own palette.
use rogue_assets::{Color, Sprite};

fn hex(s: &str) -> Color {
    let v = u32::from_str_radix(s.trim_start_matches('#'), 16).unwrap();
    [(v >> 16) as u8, (v >> 8) as u8, v as u8, 255]
}

fn from_map(name: &str, rows: &[&str], pal: &[(char, &str)]) -> Sprite {
    let mut s = Sprite::new(name, 16, 16);
    for (y, row) in rows.iter().enumerate() {
        assert_eq!(row.chars().count(), 16, "{name} row {y}: {row:?}");
        for (x, ch) in row.chars().enumerate() {
            if ch == '.' {
                continue;
            }
            let c = pal.iter().find(|(k, _)| *k == ch).unwrap_or_else(|| panic!("{name}: no colour for {ch:?}")).1;
            s.set(0, x as i32, y as i32, hex(c));
        }
    }
    assert_eq!(rows.len(), 16, "{name} must have 16 rows");
    s
}

fn noise(x: i32, y: i32, seed: i32) -> i32 {
    let mut h = (x as u32).wrapping_mul(374761393).wrapping_add((y as u32).wrapping_mul(668265263)).wrapping_add((seed as u32).wrapping_mul(1274126177));
    h = (h ^ (h >> 13)).wrapping_mul(1274126177);
    ((h ^ (h >> 16)) & 0xff) as i32
}

fn floor() -> Sprite {
    let mut s = Sprite::new("floor", 16, 16);
    for y in 0..16 {
        for x in 0..16 {
            let n = noise(x, y, 1);
            let base = if n < 40 { "#2a3040" } else if n > 220 { "#3e4659" } else { "#343b4d" };
            let seam = x % 8 == 0 || y % 8 == 0;
            s.set(0, x, y, hex(if seam { "#2a3040" } else { base }));
        }
    }
    s
}

fn wall() -> Sprite {
    let mut s = Sprite::new("wall", 16, 16);
    for y in 0..16 {
        let row = y / 4;
        let off = if row % 2 == 0 { 0 } else { 4 };
        for x in 0..16 {
            let mortar = y % 4 == 3 || (x + off) % 8 == 7;
            let n = noise(x, y, 7 + row);
            let c = if mortar { "#2a3040" } else if y % 4 == 0 { "#7e89a3" } else if n > 200 { "#6c768e" } else { "#5f6980" };
            s.set(0, x, y, hex(c));
        }
    }
    s
}

fn stairs() -> Sprite {
    let mut s = floor();
    s.name = "stairs_down".into();
    for i in 0..5 {
        let c = ["#f5b942", "#c98f2a", "#9a6a1c", "#6b4812", "#3d280a"][i];
        for y in 2 + i * 2..14 {
            for x in 2 + i..14 - i {
                if y < 2 + i * 2 + 2 {
                    s.set(0, x as i32, y as i32, hex(c));
                }
            }
        }
    }
    s
}

pub fn all() -> Vec<Sprite> {
    let hero = from_map("hero", &[
        "................",
        "......gggg......",
        ".....gGGGGg.....",
        ".....gGGGGg.....",
        ".....gsssSg.....",
        ".....gsesesg....",
        ".....gsssssg....",
        "......ssss......",
        "....bbbbbbbb....",
        "...bBbbyybbBb...",
        "...sBbbbbbbBs.w.",
        "...s.bbbbbb.s.w.",
        ".....bBbbBb...w.",
        ".....bb..bb...y.",
        ".....kk..kk.....",
        "....kkk..kkk....",
    ], &[('g', "#9aa4b5"), ('G', "#c4ccda"), ('s', "#f2c29b"), ('S', "#d9a47d"), ('e', "#1b1b24"), ('b', "#3d6fd1"), ('B', "#27458a"), ('y', "#f5b942"), ('k', "#3a2a1a"), ('w', "#e8eef7")]);
    let goblin = from_map("goblin", &[
        "................",
        "................",
        "..gg........gg..",
        "..gGg......gGg..",
        "...gGggggggGg...",
        "....gggggggg....",
        "....grrggrrg....",
        "....gggggggg....",
        ".....gMMMMg.....",
        "....bbbbbbbb....",
        "...gbBbbbbBbg...",
        "...g.bbbbbb.g...",
        ".....bBbbBb.....",
        ".....bb..bb.....",
        ".....kk..kk.....",
        "....kkk..kkk....",
    ], &[('g', "#6fbf5f"), ('G', "#4e9a45"), ('r', "#ff4d4d"), ('M', "#2a3a24"), ('b', "#7a5230"), ('B', "#573a22"), ('k', "#3a2a1a")]);
    let rat = from_map("rat", &[
        "................",
        "................",
        "................",
        "................",
        "................",
        "......pp........",
        ".....pgggg......",
        "....pggggggg....",
        "...ggeggggggg...",
        "..nggggggggggt..",
        "...gggggggggt...",
        "....gdgddgdt....",
        "....dd.dd.d.t...",
        "................",
        "................",
        "................",
    ], &[('g', "#9b8468"), ('d', "#6e5b44"), ('p', "#e8a0b0"), ('e', "#1b1b24"), ('n', "#e8a0b0"), ('t', "#e8a0b0")]);
    let skeleton = from_map("skeleton", &[
        "................",
        ".....wwwwww.....",
        "....wwwwwwww....",
        "....wkkwwkkw....",
        "....wkkwwkkw....",
        ".....wwwwww.....",
        ".....wTwTwT.....",
        "......wwww......",
        "....wwwwwwww....",
        "...w.wWwwWw.w...",
        "...w.wwwwww.w...",
        "...w.wWwwWw.w...",
        "......wwww......",
        ".....ww..ww.....",
        ".....w....w.....",
        "....ww....ww....",
    ], &[('w', "#dfe3ea"), ('W', "#aab2c0"), ('k', "#14161c"), ('T', "#b8bfcc")]);
    let orc = from_map("orc", &[
        "................",
        "....gggggggg....",
        "...gGggggggGg...",
        "...ggrggggrgg...",
        "...gggggggggg...",
        "...gwgggggwgg...",
        "....gggMMggg....",
        "...aaaaaaaaaa...",
        "..aAaaaaaaaaAa..",
        ".gaAaaaayaaaAag.",
        ".gg.aaaaaaaa.gg.",
        ".g..aAaaaaAa..g.",
        "....aaaaaaaa....",
        "....aa....aa....",
        "...kkk....kkk...",
        "..kkkk....kkkk..",
    ], &[('g', "#4f9d5f"), ('G', "#3a7a48"), ('r', "#ff4d4d"), ('w', "#f5f0e0"), ('M', "#25402b"), ('a', "#5b4a6e"), ('A', "#3f334f"), ('y', "#f5b942"), ('k', "#2a2018")]);
    let slime = from_map("slime", &[
        "................",
        "................",
        "................",
        "................",
        "......gggg......",
        "....gghhhhgg....",
        "...ghhhhhhhhg...",
        "..ghhhhhhhhhhg..",
        "..ghheehhheehg..",
        ".gghheehhheehgg.",
        ".ghhhhhhhhhhhhg.",
        ".ghhhhhmmhhhhhg.",
        ".gghhhhhhhhhhgg.",
        "..gggggggggggg..",
        "................",
        "................",
    ], &[('g', "#3a9d6a"), ('h', "#62d48f"), ('e', "#10241a"), ('m', "#2a7a52")]);
    let potion = from_map("potion", &[
        "................",
        "................",
        "......kkkk......",
        "......kccK......",
        ".......gg.......",
        ".......gg.......",
        "......gggg......",
        ".....gRRRRg.....",
        "....gRrRRRRg....",
        "....gRrRRRRg....",
        "....gRRRRRRg....",
        "....gRRRRRRg....",
        ".....gRRRRg.....",
        "......gggg......",
        "................",
        "................",
    ], &[('k', "#6b4b2a"), ('K', "#4e361d"), ('c', "#a87a45"), ('g', "#cfe3f5"), ('R', "#e5484d"), ('r', "#ff9aa0")]);
    let sword = from_map("sword", &[
        "................",
        "..............ww",
        ".............wWw",
        "............wWw.",
        "...........wWw..",
        "..........wWw...",
        ".........wWw....",
        "..h.....wWw.....",
        "..hh...wWw......",
        "...hh.wWw.......",
        "....hhyw........",
        ".....hyy........",
        "....kkhh........",
        "...kk..h........",
        "..kk............",
        "................",
    ], &[('w', "#e8eef7"), ('W', "#a9b6c9"), ('h', "#d89b3a"), ('y', "#f5b942"), ('k', "#6b4b2a")]);
    let armor = from_map("armor", &[
        "................",
        "................",
        "...bb......bb...",
        "..bBBb....bBBb..",
        "..bBBbbbbbbBBb..",
        "..bBbbBbbBbbBb..",
        "...bbbBbbBbbb...",
        "....bbbbbbbb....",
        "....bBbbbbBb....",
        "....bbBbbBbb....",
        "....bbbbbbbb....",
        "....bBbbbbBb....",
        "....bbbbbbbb....",
        ".....bbbbbb.....",
        "................",
        "................",
    ], &[('b', "#b8864b"), ('B', "#8a6232")]);
    vec![floor(), wall(), stairs(), hero, goblin, rat, skeleton, orc, slime, potion, sword, armor]
}
