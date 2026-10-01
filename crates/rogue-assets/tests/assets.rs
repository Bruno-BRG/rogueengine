use rogue_assets::*;

const RED: Color = [255, 0, 0, 255];
const BLUE: Color = [0, 0, 255, 255];

#[test]
fn drawing_tools_and_undo_redo() {
    let mut ed = SpriteEditor::new(Sprite::new("hero", 8, 8));
    ed.rect((0, 0), (7, 7), RED, false);
    assert_eq!(ed.sprite.get(0, 0, 3), Some(RED));
    assert_eq!(ed.sprite.get(0, 3, 3), Some([0, 0, 0, 0]));
    ed.fill(3, 3, BLUE); // fills the interior only, stopped by the red border
    assert_eq!(ed.sprite.get(0, 3, 3), Some(BLUE));
    assert_eq!(ed.sprite.get(0, 0, 0), Some(RED));
    ed.line((1, 1), (6, 6), RED);
    assert_eq!(ed.sprite.get(0, 4, 4), Some(RED));
    assert!(ed.undo());
    assert_eq!(ed.sprite.get(0, 4, 4), Some(BLUE));
    assert!(ed.redo());
    assert_eq!(ed.sprite.get(0, 4, 4), Some(RED));
    ed.pixel(100, 100, RED); // out of bounds is ignored, not a panic
}

#[test]
fn flip_and_frames() {
    let mut ed = SpriteEditor::new(Sprite::new("s", 4, 2));
    ed.pixel(0, 0, RED);
    ed.flip_horizontal();
    assert_eq!(ed.sprite.get(0, 3, 0), Some(RED));
    ed.add_frame(true);
    assert_eq!(ed.sprite.frames.len(), 2);
    assert_eq!(ed.sprite.get(1, 3, 0), Some(RED));
    assert!(ed.remove_frame());
    assert!(!ed.remove_frame());
}

#[test]
fn png_roundtrip_with_sheet_slicing() {
    let mut ed = SpriteEditor::new(Sprite::new("s", 4, 4));
    ed.pixel(1, 1, RED);
    ed.add_frame(false);
    ed.pixel(2, 2, BLUE);
    let png = ed.sprite.sheet_png().unwrap();
    let back = Sprite::from_png("s", &png, Some(4)).unwrap();
    assert_eq!(back.frames, ed.sprite.frames);
    assert!(Sprite::from_png("s", &png, Some(3)).is_err());
}

fn clip(name: &str, n: usize, ms: u32, looping: bool) -> Clip {
    Clip { name: name.into(), frames: (0..n).map(|i| ClipFrame { frame: i, ms }).collect(), looping }
}

#[test]
fn animator_loops_and_one_shots() {
    let mut a = Animator::new(vec![clip("idle", 2, 100, true), clip("hit", 2, 100, false)]);
    a.play("idle", false);
    assert_eq!(a.frame(), Some(0));
    a.update(150);
    assert_eq!(a.frame(), Some(1));
    a.update(100);
    assert_eq!(a.frame(), Some(0));
    a.play("hit", true);
    a.update(1000);
    assert!(a.finished());
    assert_eq!(a.frame(), Some(1));
    assert!(!a.play("missing", false));
}

#[test]
fn controller_transitions() {
    let mut c = Controller::new(
        vec![clip("idle", 1, 100, true), clip("attack", 2, 50, false)],
        vec![Transition { from: "idle".into(), to: "attack".into(), on: "attack".into() }, Transition { from: "attack".into(), to: "idle".into(), on: "finished".into() }],
        "idle",
    );
    assert!(c.trigger("attack"));
    assert_eq!(c.animator.current(), Some("attack"));
    c.update(200);
    assert_eq!(c.animator.current(), Some("idle"));
    assert!(!c.trigger("nonsense"));
}

#[test]
fn ops_are_json_driven() {
    let mut ed = SpriteEditor::new(Sprite::new("s", 4, 4));
    for op in [
        r#"{"op":"pixel","x":1,"y":1,"color":[255,0,0,255]}"#,
        r#"{"op":"add_frame","duplicate":true}"#,
        r#"{"op":"rect","x0":0,"y0":0,"x1":3,"y1":3,"color":[0,0,255,255],"filled":false}"#,
        r#"{"op":"select_frame","index":0}"#,
    ] {
        ed.apply(serde_json::from_str(op).unwrap());
    }
    let v = ed.view();
    assert_eq!((v.frame, v.frame_count), (0, 2));
    assert_eq!(&v.pixels[(1 * 4 + 1) * 4..(1 * 4 + 1) * 4 + 4], &[255, 0, 0, 255]);
    assert_eq!(&v.pixels[0..4], &[0, 0, 0, 0], "rect was drawn on frame 1, not frame 0");
    ed.apply(serde_json::from_str(r#"{"op":"undo"}"#).unwrap());
}
