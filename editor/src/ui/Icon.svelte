<script lang="ts">
  let { name, size = 16 }: { name: string; size?: number } = $props();

  const P: Record<string, string> = {
    play: "M7 4l13 8-13 8z",
    plus: "M5 12h14 M12 5v14",
    x: "M18 6 6 18 M6 6l12 12",
    trash: "M3 6h18 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6 M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2",
    pencil: "M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z",
    eraser: "m7 21-4.3-4.3c-1-1-1-2.5 0-3.4l9.6-9.6c1-1 2.5-1 3.4 0l5.6 5.6c1 1 1 2.5 0 3.4L13 21 M22 21H7 M5 11l9 9",
    undo: "M3 7v6h6 M21 17a9 9 0 0 0-9-9 9 9 0 0 0-6 2.3L3 13",
    redo: "M21 7v6h-6 M3 17a9 9 0 0 1 9-9 9 9 0 0 1 6 2.3L21 13",
    square: "M4 4h16v16H4z",
    line: "M5 19 19 5",
    bucket: "m19 11-8-8-8.6 8.6a2 2 0 0 0 0 2.8l5.2 5.2c.8.8 2 .8 2.8 0L19 11Z M5 2l5 5 M2 13h15 M22 20a2 2 0 1 1-4 0c0-1.6 1.7-2.4 2-4 .3 1.6 2 2.4 2 4Z",
    pipette: "m2 22 1-1h3l9-9 M3 21v-3l9-9 M15 6l3.4-3.4a2.1 2.1 0 1 1 3 3L18 9l.4.4a2.1 2.1 0 1 1-3 3l-3.8-3.8a2.1 2.1 0 1 1 3-3l.4.4Z",
    flip: "M8 3H5a2 2 0 0 0-2 2v14c0 1.1.9 2 2 2h3 M16 3h3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-3 M12 20v2 M12 14v2 M12 8v2 M12 2v2",
    grid: "M3 3h18v18H3z M3 9h18 M3 15h18 M9 3v18 M15 3v18",
    folder: "M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z",
    code: "m16 18 6-6-6-6 M8 6l-6 6 6 6",
    image: "M3 3h18v18H3z M8.5 8.5h.01 M21 15l-5-5L5 21",
    graph: "M3 4h7v6H3z M14 14h7v6h-7z M10 7h2a3 3 0 0 1 3 3v4",
    box: "M21 8 12 3 3 8v8l9 5 9-5z M3.3 7 12 12l8.7-5 M12 22V12",
    search: "M11 3a8 8 0 1 0 0 16 8 8 0 0 0 0-16z M21 21l-4.3-4.3",
    copy: "M8 8h12v12H8z M4 16V4h12",
    heart: "M19 14c1.5-1.5 3-3.2 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.8 0-3 .5-4.5 2-1.5-1.5-2.7-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4 3 5.5l7 7Z",
    download: "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M7 10l5 5 5-5 M12 15V3",
    upload: "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4 M17 8l-5-5-5 5 M12 3v12",
    zap: "M13 2 3 14h9l-1 8 10-12h-9z",
    chevron: "m6 9 6 6 6-6",
    check: "M20 6 9 17l-5-5",
    terminal: "m4 17 6-6-6-6 M12 19h8",
    layers: "m12 2 10 5-10 5L2 7z M2 17l10 5 10-5 M2 12l10 5 10-5",
    file: "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6",
    book: "M4 19.5A2.5 2.5 0 0 1 6.5 17H20 M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5Z",
    sword: "M14.5 17.5 3 6V3h3l11.5 11.5 M13 19l6-6 M16 16l4 4 M19 21l2-2",
    refresh: "M21 12a9 9 0 1 1-3-6.7L21 8 M21 3v5h-5",
    eye: "M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6",
    home: "M3 11 12 3l9 8 M5 10v10h14V10",
    bag: "M6 7h12l1 14H5z M9 7a3 3 0 0 1 6 0",
    shield: "M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z",
    fit: "M4 9V4h5 M20 9V4h-5 M4 15v5h5 M20 15v5h-5",
    wand: "m15 4 5 5L8 21l-5-5z M14 5l5 5",
    arrow: "M5 12h14 M13 6l6 6-6 6",
    lock: "M6 11h12v10H6z M8 11V7a4 4 0 0 1 8 0v4",
  };
  const paths = $derived((P[name] ?? P.file).split(" M").map((d, i) => (i ? "M" + d : d)));
</script>

<svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
  {#each paths as d}<path {d} />{/each}
</svg>
