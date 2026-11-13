namespace Viewer.Tests.Browser;

public static class Scripts
{
    public const string EdgesThroughCards = """
        () => {
          const boxes = [...document.querySelectorAll('g.card')].map(c => {
            const r = c.querySelector('rect');
            const t = c.getAttribute('transform').match(/-?[\d.]+/g).map(Number);
            const l = +r.getAttribute('x') + t[0], top = +r.getAttribute('y') + t[1];
            return { l, t: top, r: l + +r.getAttribute('width'), b: top + +r.getAttribute('height') };
          });
          const near = (p, b) => p[0] >= b.l - 3 && p[0] <= b.r + 3 && p[1] >= b.t - 3 && p[1] <= b.b + 3;
          let count = 0;
          for (const path of document.querySelectorAll('path[marker-end]')) {
            const d = path.getAttribute('d'), pts = [];
            for (const m of d.matchAll(/([MLQ])([^MLQ]+)/g)) {
              const n = m[2].trim().split(/[ ,]+/).map(Number);
              if (m[1] === 'M') pts.push(n);
              if (m[1] === 'Q') pts.push(n.slice(0, 2));
            }
            pts.push(d.match(/L([^MLQ]+)$/)[1].split(',').map(Number));
            const ends = boxes.filter(b => near(pts[0], b) || near(pts.at(-1), b));
            const hit = pts.slice(1).some((q, i) => {
              const p = pts[i];
              const s = { l: Math.min(p[0], q[0]), t: Math.min(p[1], q[1]) };
              s.r = Math.max(p[0], q[0]);
              s.b = Math.max(p[1], q[1]);
              return boxes.some(b => !ends.includes(b)
                && s.l < b.r - 2 && b.l + 2 < s.r && s.t < b.b - 2 && b.t + 2 < s.b);
            });
            if (hit) count++;
          }
          return count;
        }
        """;

    public const string Cards = """
        () => [...document.querySelectorAll('g.card')].map(c => {
          const r = c.querySelector('rect');
          const t = c.getAttribute('transform').match(/-?[\d.]+/g).map(Number);
          return [c.textContent.replace(/\s+/g, ' '), +r.getAttribute('x') + t[0], +r.getAttribute('y') + t[1]];
        })
        """;

    public const string ReachableHead = """
        () => {
          const cards = [...document.querySelectorAll('svg.canvas g.card')];
          const ends = [...document.querySelectorAll('svg.canvas path[marker-end]')].flatMap(p => {
            const m = p.getScreenCTM(), n = p.getTotalLength();
            return [p.getPointAtLength(0), p.getPointAtLength(n)].map(q => new DOMPoint(q.x, q.y).matrixTransform(m));
          });
          const touched = c => {
            const r = c.getBoundingClientRect();
            return ends.some(q => q.x >= r.left - 8 && q.x <= r.right + 8 && q.y >= r.top && q.y <= r.bottom);
          };
          for (let i = 0; i < cards.length; i++) {
            const head = cards[i].querySelector('g.head');
            if (!head || !touched(cards[i])) continue;
            const r = head.getBoundingClientRect();
            const x = r.left + 20, y = r.top + 10;
            if (x < 0 || y < 0 || x > innerWidth - 80 || y > innerHeight - 80) continue;
            const hit = document.elementFromPoint(x, y);
            if (hit && cards[i].contains(hit)) return [i, x, y];
          }
          return [-1, 0, 0];
        }
        """;

    public const string RowPlaces = """
        c => {
          const top = +c.querySelector('rect').getAttribute('y');
          return [...c.querySelectorAll('g.row > rect')]
            .map(r => `${+r.getAttribute('y') - top}/${r.getAttribute('height')}`).join(' ');
        }
        """;

    public const string BackgroundY = """
        () => {
          for (let y = 1100; y > 0; y -= 10) {
            const e = document.elementFromPoint(1000, y);
            if (e.tagName === 'rect' && e.parentElement.tagName === 'svg') return y;
          }
          return -1;
        }
        """;

    public const string BackgroundPoint = """
        () => {
          for (let y = 1100; y > 100; y -= 10) for (let x = 1900; x > 100; x -= 50) {
            const e = document.elementFromPoint(x, y);
            if (e && e.tagName === 'rect' && e.parentElement.tagName === 'svg') return [x, y];
          }
          return [-1, -1];
        }
        """;

    public const string FreePair = """
        () => {
          const cards = [...document.querySelectorAll('g.card')]
            .map(c => ({ id: c.dataset.node, box: c.querySelector('rect').getBoundingClientRect() }));
          const linked = new Set([...document.querySelectorAll('path.edge-hit')]
            .map(p => p.dataset.from + '>' + p.dataset.to));
          let best = null;
          for (const a of cards) for (const b of cards) {
            if (a === b || linked.has(a.id + '>' + b.id) || linked.has(b.id + '>' + a.id)) continue;
            if (b.box.left - a.box.right < 40) continue;
            const d = Math.hypot(b.box.left - a.box.right, b.box.top - a.box.top);
            if (!best || d < best.d) best = { from: a.id, to: b.id, d };
          }
          return [best.from, best.to];
        }
        """;

    public const string EndsOnPorts = """
        ([id, fromNode, fromRow, toNode, toRow]) => {
          const d = document.querySelector(`path[marker-end][data-edge="${id}"]`).getAttribute('d');
          const pts = [...d.matchAll(/-?[\d.]+/g)].map(Number);
          const port = (node, side, row) => document.querySelector(
            `g.card[data-node="${node}"] ${row} circle.port-hit[data-side="${side}"]`);
          const near = (c, x, y) => Math.abs(+c.getAttribute('cx') - x) <= 3
            && Math.abs(+c.getAttribute('cy') - y) <= 3;
          return near(port(fromNode, 'Out', fromRow), pts[0], pts[1])
            && near(port(toNode, 'In', toRow), pts.at(-2), pts.at(-1));
        }
        """;

    public const string EdgeMiddle = """
        p => {
          const q = p.getPointAtLength(p.getTotalLength() / 2).matrixTransform(p.getScreenCTM());
          return [q.x, q.y];
        }
        """;

    public const string VisibleEdgePoint = """
        p => {
          for (let i = 0; i <= 18; i++) {
            const f = 0.5 + (i % 2 ? 1 : -1) * Math.ceil(i / 2) * 0.05;
            const q = p.getPointAtLength(p.getTotalLength() * f).matrixTransform(p.getScreenCTM());
            if (document.elementFromPoint(q.x, q.y) === p) return [q.x, q.y];
          }
          return [-1, -1];
        }
        """;

    public const string PartRows = """
        c => {
          const top = +c.querySelector('rect').getAttribute('y');
          return [...c.querySelectorAll('g.part-row circle')].map(r => +r.getAttribute('cy') - top).join(' ')
            + ' h=' + c.querySelector('rect').getAttribute('height');
        }
        """;

    public const string RemovedLook = """
        p => {
          const text = p.parentElement.querySelector('.edge-label text');
          return [p.parentElement.getAttribute('opacity'), p.getAttribute('stroke-dasharray'),
            text?.textContent, text?.getAttribute('text-decoration')].join('|');
        }
        """;

    public const string RoutesChanged = """
        r => [...document.querySelectorAll('path[marker-end]')].map(p => p.getAttribute('d')).join('|') !== r
        """;

    public const string EdgeRemoved = """
        id => document.querySelector(`path[marker-end][data-edge="${id}"]`)?.dataset.removed === 'true'
        """;

    public const string EdgeAccess = """
        ([id, a]) => document.querySelector(`path[marker-end][data-edge="${id}"]`)?.dataset.access === a
        """;

    public const string NewRuleDrawn = """
        () => [...document.querySelectorAll('path[marker-end]')].some(p => p.dataset.edge.startsWith('new:'))
        """;

    public const string ChangesStartWith = """
        t => document.querySelector('.toolbar .changes').textContent.trim().startsWith(t)
        """;
}
