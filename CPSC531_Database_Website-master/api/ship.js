export default async function handler(req, res) {
  const r = await fetch('http://34.145.122.102:8000/ship');
  const data = await r.json();
  res.status(200).json(data);
}