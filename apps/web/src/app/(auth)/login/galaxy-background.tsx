"use client";
import { useEffect, useRef, useState } from "react";
import { Pause, Play } from "lucide-react";
import styles from "./login.module.css";

export function GalaxyBackground() {
  const canvas = useRef<HTMLCanvasElement>(null);
  const [paused, setPaused] = useState(false);
  const [reducedMotion, setReducedMotion] = useState(false);
  useEffect(() => {
    const preference = matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => setReducedMotion(preference.matches);
    update(); preference.addEventListener("change", update);
    return () => preference.removeEventListener("change", update);
  }, []);
  useEffect(() => {
    const element = canvas.current;
    const ctx = element?.getContext("2d");
    if (!element || !ctx) return;
    let seed = 72031;
    const random = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };
    const stars = Array.from({ length: 220 }, () => ({ x: random(), y: random(), radius: .35 + random() * 1.15, phase: random() * 6.28 }));
    const galaxy = document.createElement("canvas"); galaxy.width = galaxy.height = 800;
    const g = galaxy.getContext("2d");
    if (!g) return;
    const glow = g.createRadialGradient(400, 400, 0, 400, 400, 360);
    glow.addColorStop(0, "#fff0dcbb"); glow.addColorStop(.08, "#eec5e778"); glow.addColorStop(.3, "#ad62952a"); glow.addColorStop(1, "#512e8000");
    g.fillStyle = glow; g.fillRect(0, 0, 800, 800);
    for (let i = 0; i < 14500; i++) {
      const radius = Math.pow(random(), .7) * 350;
      const angle = radius / 74 + (i % 3) * Math.PI * 2 / 3 + (random() - .5) * (.45 + radius / 500);
      const x = 400 + Math.cos(angle) * radius + (random() - .5) * 16;
      const y = 400 + Math.sin(angle) * radius + (random() - .5) * 16;
      g.fillStyle = i % 5 === 0 ? "#d2d6ff9a" : i % 3 === 0 ? "#f5c8d879" : "#c594d04c";
      g.beginPath(); g.arc(x, y, random() * 1.2 + .25, 0, Math.PI * 2); g.fill();
    }
    let width = 0, height = 0, frame = 0, last = 0, time = 0;
    const resize = () => { width = innerWidth; height = innerHeight; const ratio = Math.min(devicePixelRatio || 1, 1.5); element.width = width * ratio; element.height = height * ratio; ctx.setTransform(ratio, 0, 0, ratio, 0, 0); draw(); };
    function draw() {
      if (!ctx) return;
      ctx.clearRect(0, 0, width, height);
      const paintGalaxy = (x: number, y: number, size: number, rotation: number, opacity: number) => {
        ctx.save(); ctx.translate(x, y); ctx.rotate(rotation); ctx.scale(1, .58); ctx.globalAlpha = opacity; ctx.drawImage(galaxy, -size / 2, -size / 2, size, size); ctx.restore();
      };
      paintGalaxy(width * .16, height * .32, Math.min(width * .8, 940), -.55 + time * .000012, .85);
      paintGalaxy(width * .86, height * .74, Math.min(width * .6, 680), .4 - time * .000016, .65);
      paintGalaxy(width * .81, height * .17, 280, -.5 + time * .000009, .45);
      paintGalaxy(width * .12, height * .83, 260, .2, .4);
      ctx.save(); ctx.translate(width * .83, height * .43); ctx.scale(1, .48);
      for (let orbit=1;orbit<=4;orbit++) { const radius=orbit*25; ctx.strokeStyle='#a5bcff28'; ctx.beginPath(); ctx.arc(0,0,radius,0,Math.PI*2); ctx.stroke(); const angle=orbit*1.8+time*.00003/orbit; ctx.fillStyle=['','#7cbced','#cda7ee','#edbe92','#96d6c4'][orbit]; ctx.beginPath(); ctx.arc(Math.cos(angle)*radius,Math.sin(angle)*radius,3+orbit,0,Math.PI*2); ctx.fill(); }
      ctx.fillStyle='#f4d9a3'; ctx.shadowColor='#f3be74'; ctx.shadowBlur=24; ctx.beginPath(); ctx.arc(0,0,10,0,Math.PI*2); ctx.fill(); ctx.restore();
      for (const star of stars) {
        const alpha = .35 + .45 * (.5 + .5 * Math.sin(time * .00065 + star.phase));
        ctx.fillStyle = `rgba(238,225,250,${alpha})`;
        ctx.beginPath(); ctx.arc((star.x * width + time * .0015) % width, star.y * height, star.radius, 0, Math.PI * 2); ctx.fill();
      }
    }
    const animate = (now: number) => { if (now - last >= 33) { time += last ? Math.min(now - last, 70) : 0; last = now; draw(); } frame = requestAnimationFrame(animate); };
    const start = () => { cancelAnimationFrame(frame); last = 0; if (!paused && !reducedMotion && !document.hidden) frame = requestAnimationFrame(animate); else draw(); };
    resize(); start(); addEventListener("resize", resize); document.addEventListener("visibilitychange", start);
    return () => { cancelAnimationFrame(frame); removeEventListener("resize", resize); document.removeEventListener("visibilitychange", start); };
  }, [paused, reducedMotion]);
  return <><canvas ref={canvas} className={styles.cosmos} aria-hidden="true" />{!reducedMotion && <button className={styles.motion} type="button" onClick={() => setPaused(value => !value)} aria-label={paused ? "Play background animation" : "Pause background animation"}>{paused ? <Play size={13} aria-hidden="true" /> : <Pause size={13} aria-hidden="true" />}<span>{paused ? "Play motion" : "Pause motion"}</span></button>}</>;
}
