export default async function handler(req, res) {
  const { date, cruise_line = "", ship_name = "" } = req.query;

  const url = new URL("http://34.145.122.102:8000/raw");
  if (date) url.searchParams.set("date", date);
  if (cruise_line) url.searchParams.set("cruise_line", cruise_line);
  if (ship_name) url.searchParams.set("ship_name", ship_name);

  try {
    const r = await fetch(url.toString());
    const data = await r.json();
    res.status(200).json(data);
  } catch (err) {
    res.status(500).json({ error: "Backend unreachable", details: err.toString() });
  }
}