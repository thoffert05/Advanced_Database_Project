export default async function handler(req, res) {
  const { start, end, ship = "true", cruise = "true", global = "true" } = req.query;

  const url = new URL("http://34.145.122.102:8000/summary");
  if (start) url.searchParams.set("start", start);
  if (end) url.searchParams.set("end", end);
  url.searchParams.set("ship", ship);
  url.searchParams.set("cruise", cruise);
  url.searchParams.set("global", global);

  try {
    const r = await fetch(url.toString());
    const data = await r.json();
    res.status(200).json(data);
  } catch (err) {
    res.status(500).json({ error: "Backend unreachable", details: err.toString() });
  }
}