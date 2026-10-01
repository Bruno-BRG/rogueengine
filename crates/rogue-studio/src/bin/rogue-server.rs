use std::path::PathBuf;

fn main() {
    let mut args = std::env::args().skip(1);
    let addr = args.next().unwrap_or_else(|| "127.0.0.1:1430".into());
    let static_dir = args.next().map(PathBuf::from);
    let studio = rogue_studio::Studio::new(rogue_host::Host::spawn(None));
    eprintln!("RogueEngine studio server on http://{addr}");
    if let Err(e) = rogue_studio::server::serve(studio, &addr, static_dir) {
        eprintln!("error: {e}");
        std::process::exit(1);
    }
}
