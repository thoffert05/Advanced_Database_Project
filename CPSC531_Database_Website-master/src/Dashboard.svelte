<script>
  import { onMount } from 'svelte';

  // ── Constants 
  const LINES = ['All','Royal Caribbean International','Carnival Cruise Line','Norwegian Cruise Line','Princess Cruises','Celebrity Cruises','Holland America Line','MSC Cruises','Disney Cruise Line','Oceania Cruises','Regent Seven Seas Cruises','Azamara','American Queen Voyages','American Cruise Lines','Victory Cruise Lines','Pearl Seas Cruises'];
  const COLORS = {'Royal Caribbean International':'#003087','Carnival Cruise Line':'#e31837','Norwegian Cruise Line':'#00205b','Princess Cruises':'#4169a0','Celebrity Cruises':'#1b3a6b','Holland America Line':'#4a6e8e','MSC Cruises':'#003399','Disney Cruise Line':'#1a237e','Oceania Cruises':'#8B4513','Regent Seven Seas Cruises':'#4a0080','Azamara':'#c8860a','American Queen Voyages':'#8b0000','American Cruise Lines':'#006400','Victory Cruise Lines':'#2e4057','Pearl Seas Cruises':'#1a6e8e'};
  const SHIP_COLS = [['Ship Name','Ship Name'],['CruiseLine','Line'],['YearBuilt','Built'],['GT','GT'],['PassengerCapacity','Pax'],['CrewCount','Crew'],['DWT','DWT'],['Speed','Kts'],['momentum','Momentum']];
  const LINE_COLS = [['CruiseLine','Cruise Line'],['shipCount','Ships'],['totalPax','Total Pax'],['totalCrew','Crew'],['avgYear','Avg Built'],['totalDWT','Total DWT'],['totalMomentum','Momentum']];

  // ── State 
  let ships = [], search = '', selectedLine = 'All';
  let sortKey = 'PassengerCapacity', sortDir = -1, selectedShip = null, activeTab = 'fleet';
  let groupBy = 'Ship', momentumView = 'overall';
  let yearFrom = 1990, yearTo = 2019;
  let lineSortKey = 'totalMomentum', lineSortDir = -1;

  // ── Helpers 
  const dot  = (l, s=8) => `background:${COLORS[l]||'#4a5880'};width:${s}px;height:${s}px;border-radius:50%;display:inline-block;margin-right:6px;flex-shrink:0`;
  const sum  = (a, k)   => a.reduce((t, x) => t + (x[k]||0), 0);
  const fmtM = v => v >= 1e9 ? (v/1e9).toFixed(2)+'B' : v >= 1e6 ? (v/1e6).toFixed(1)+'M' : v >= 1e3 ? (v/1e3).toFixed(0)+'K' : String(Math.round(v));

  // Group array by key, sum momentum — returns [[key, total], ...] sorted desc
  const gmom = (arr, key) =>
    Object.entries(arr.reduce((g, s) => { g[s[key]] = (g[s[key]]||0) + s.momentum; return g; }, {}))
          .sort((a, b) => b[1] - a[1]);

  // Group array by a time key function — returns [[key, total, count], ...] sorted asc
  const gtime = (arr, fn) =>
    Object.entries(arr.reduce((g, s) => { const k = fn(s); if (!g[k]) g[k] = {t:0,n:0}; g[k].t += s.momentum; g[k].n++; return g; }, {}))
          .map(([k, {t, n}]) => [+k, t, n]).sort((a, b) => a[0] - b[0]);

  const cmpSort = (dir, key) => (a, b) => {
    const av = a[key], bv = b[key];
    return typeof av === 'string' ? dir * av.localeCompare(bv||'') : dir * ((av||0) - (bv||0));
  };

  function sortBy(k)     { sortKey === k     ? sortDir     = -sortDir     : (sortKey     = k, sortDir     = -1); }
  function sortLineBy(k) { lineSortKey === k ? lineSortDir = -lineSortDir : (lineSortKey = k, lineSortDir = -1); }

  // ── Data loading 
async function loadData() {
  const res = await fetch('http://34.145.122.102:8000/ship');
  const data = await res.json();

  ships = data.map(s => ({
    ...s,

    // Normalize field names to match your UI expectations
    ['Ship Name']: s.ShipName,   // your UI expects this exact key

    // Ensure numeric fields are numbers
    DWT: +s.DWT,
    GT: +s.GT,
    YearBuilt: +s.YearBuilt,
    PassengerCapacity: +s.PassengerCapacity,
    CrewCount: +s.CrewCount,

    // Derived fields
    Speed: 1,                    // your JSON has no Speed; set placeholder
    momentum: (+s.DWT) * 1 * 1000
  }));
}
  onMount(loadData);

  // ── Reactive: fleet 
  $: baseFiltered = ships
    .filter(s => selectedLine === 'All' || s.CruiseLine === selectedLine)
    .filter(s => !search || s['Ship Name'].toLowerCase().includes(search.toLowerCase()));

  $: filtered   = [...baseFiltered].sort(cmpSort(sortDir, sortKey));
  $: lineGroups = Object.values(
      baseFiltered.reduce((g, s) => {
        if (!g[s.CruiseLine]) g[s.CruiseLine] = { CruiseLine:s.CruiseLine, shipCount:0, totalPax:0, totalCrew:0, totalYear:0, totalDWT:0, totalMomentum:0 };
        const r = g[s.CruiseLine];
        r.shipCount++; r.totalPax+=s.PassengerCapacity; r.totalCrew+=s.CrewCount;
        r.totalYear+=s.YearBuilt; r.totalDWT+=s.DWT; r.totalMomentum+=s.momentum;
        return g;
      }, {})
    ).map(r => ({ ...r, avgYear: Math.round(r.totalYear/r.shipCount) }))
     .sort(cmpSort(lineSortDir, lineSortKey));

  $: st = filtered.length ? {
    total:    filtered.length,
    pax:      sum(filtered,'PassengerCapacity').toLocaleString(),
    crew:     sum(filtered,'CrewCount').toLocaleString(),
    year:     Math.round(sum(filtered,'YearBuilt') / filtered.length),
    ratio:    (sum(filtered,'PassengerCapacity') / sum(filtered,'CrewCount')).toFixed(1),
    big:      filtered.reduce((a,b) => a.GT > b.GT ? a : b),
    totalMom: sum(filtered,'momentum'),
    topMom:   filtered.reduce((a,b) => (a.momentum||0) > (b.momentum||0) ? a : b)
  } : null;

  // ── Reactive: momentum 
  $: momBase = ships
    .filter(s => selectedLine === 'All' || s.CruiseLine === selectedLine);

  $: byShipMom  = [...momBase].sort((a,b) => b.momentum - a.momentum).slice(0,15);
  $: byLineMom  = gmom(momBase, 'CruiseLine');
  $: byDecadeMom = gtime(momBase, s => Math.floor(s.YearBuilt/10)*10);
  $: byYearMom   = gtime(momBase, s => s.YearBuilt);

  $: yr0 = Math.min(yearFrom, yearTo);
  $: yr1 = Math.max(yearFrom, yearTo);
  $: rangeShips  = momBase.filter(s => s.YearBuilt >= yr0 && s.YearBuilt <= yr1);
  $: byRangeShip = [...rangeShips].sort((a,b) => b.momentum - a.momentum).slice(0,15);
  $: byRangeLine = gmom(rangeShips, 'CruiseLine');

  $: maxShipMom  = byShipMom[0]?.momentum   || 1;
  $: maxLineMom  = byLineMom[0]?.[1]         || 1;
  $: maxDecMom   = Math.max(...byDecadeMom.map(([,v]) => v), 1);
  $: maxYearMom  = Math.max(...byYearMom.map(([,v])   => v), 1);
  $: maxRangeMom = groupBy==='Ship' ? (byRangeShip[0]?.momentum||1) : (byRangeLine[0]?.[1]||1);

  // ── Reactive: analytics 
  $: byLine   = Object.entries(ships.reduce((a,s) => (a[s.CruiseLine]=(a[s.CruiseLine]||0)+s.PassengerCapacity, a), {})).sort((a,b)=>b[1]-a[1]).slice(0,10);
  $: maxPax   = byLine[0]?.[1] || 1;
  $: byDecade = gtime(ships, s => Math.floor(s.YearBuilt/10)*10).map(([d,,n]) => [d+'', n]);
  $: maxDec   = Math.max(...byDecade.map(([,v]) => v), 1);
</script>

<div class="app">
  <!-- ── Sidebar ── -->
  <aside>
    <div class="logo">
      <div style="font-size:28px">⚓</div>
      <h1>Maritime<br/>Registry</h1>
      <small>NOAA 2019</small>
    </div>
    <nav>
      <div class="lbl">Navigation</div>
      {#each [['fleet','🚢 Fleet Registry'],['analytics','📊 Analytics'],['momentum','⚡ Momentum']] as [tab, label]}
        <button class="ni" class:on={activeTab===tab} on:click={()=>activeTab=tab}>{label}</button>
      {/each}
    </nav>
    <div class="filters">
      <div class="lbl">Cruise Line</div>
      {#each LINES as l}
        <button class="fb" class:on={selectedLine===l} on:click={()=>selectedLine=l} title={l}>
          {#if l!=='All'}<span style={dot(l,6)}></span>{/if}{l==='All'?'All Lines':l}
        </button>
      {/each}
    </div>
  </aside>

  <!-- ── Main ── -->
  <main>
    <div class="topbar">
      <h2>{activeTab==='fleet'?'Fleet Registry':activeTab==='analytics'?'Analytics':'Momentum'}</h2>
      <div class="tr">
        <select bind:value={groupBy}>
          <option value="Ship">By Ship</option>
          <option value="CruiseLine">By Cruise Line</option>
        </select>
        {#if activeTab !== 'momentum'}
          <input type="text" placeholder="🔍  Search ships..." bind:value={search}/>
        {/if}
      </div>
    </div>

    <div class="content">

      <!-- Stat cards -->
      {#if st}
      <div class="cards">
        <div class="card"><span class="clbl">Total Ships</span>      <div class="val">{st.total}</div>                          <small>in selection</small></div>
        <div class="card"><span class="clbl">Passengers</span>       <div class="val">{st.pax}</div>                            <small>combined berths</small></div>
        <div class="card"><span class="clbl">Total Crew</span>       <div class="val">{st.crew}</div>                           <small>crew members</small></div>
        <div class="card"><span class="clbl">Avg Build Year</span>   <div class="val">{st.year}</div>                           <small>fleet vintage</small></div>
        <div class="card"><span class="clbl">Pax / Crew</span>       <div class="val">{st.ratio}</div>                          <small>passengers per crew</small></div>
        <div class="card"><span class="clbl">Largest Ship</span>     <div class="val sm">{st.big['Ship Name']}</div>            <small>{st.big.GT.toLocaleString()} GT</small></div>
        <div class="card"><span class="clbl">Fleet Momentum</span>   <div class="val mom">{fmtM(st.totalMom)}</div>            <small>speed × DWT × 1,000</small></div>
        <div class="card"><span class="clbl">Peak Momentum</span>    <div class="val sm" style="color:#7ec8c8">{st.topMom['Ship Name']}</div><small>{fmtM(st.topMom.momentum)}</small></div>
      </div>
      {/if}

      <!-- Analytics tab -->
      {#if activeTab==='analytics'}
      <div class="charts">
        <div class="chart">
          <h3>Passenger Capacity by Line</h3>
          {#each byLine as [l, pax]}
            <div class="br"><div class="bl" title={l}><span style={dot(l,6)}></span>{l}</div><div class="bt"><div class="bf" style="width:{(pax/maxPax*100).toFixed(1)}%;background:{COLORS[l]||'#c9a84c'}"><span>{pax.toLocaleString()}</span></div></div></div>
          {/each}
        </div>
        <div class="chart">
          <h3>Ships Built by Decade</h3>
          {#each byDecade as [d, n]}
            <div class="br"><div class="bl">{d}s</div><div class="bt"><div class="bf" style="width:{(n/maxDec*100).toFixed(1)}%;background:linear-gradient(90deg,#1a4a6e,#c9a84c)"><span>{n} ships</span></div></div></div>
          {/each}
        </div>
      </div>
      {/if}

      <!-- Momentum tab -->
      {#if activeTab==='momentum'}
      <div class="mom-header">
        <div class="mom-formula"><span>speedDWT1000</span> = Speed × DWT × 1,000</div>
        <div class="mom-desc">Speed in knots · DWT = Dead Weight Tonnage · momentum combines vessel velocity and mass</div>
      </div>

      <div class="mvnav">
        {#each [['overall','Overall'],['year','Year'],['range','Date Range'],['voyage','Voyage']] as [v, label]}
          <button class="mvbtn" class:on={momentumView===v} on:click={()=>momentumView=v}>{label}</button>
        {/each}
      </div>

      {#if momentumView==='overall'}
        <div class="charts">
          <div class="chart wide">
            <h3>Overall Momentum — {groupBy==='Ship'?'Top 15 Ships':'All Cruise Lines'}</h3>
            <div class="sub">Ranked by total speedDWT1000</div>
            {#if groupBy==='Ship'}
              {#each byShipMom as s}
                <div class="br"><div class="bl" title={s['Ship Name']}><span style={dot(s.CruiseLine,6)}></span>{s['Ship Name']}</div><div class="bt"><div class="bf" style="width:{(s.momentum/maxShipMom*100).toFixed(1)}%;background:{COLORS[s.CruiseLine]||'#c9a84c'}"><span>{fmtM(s.momentum)}</span></div></div></div>
              {/each}
            {:else}
              {#each byLineMom as [line, total]}
                <div class="br"><div class="bl" title={line}><span style={dot(line,6)}></span>{line}</div><div class="bt"><div class="bf" style="width:{(total/maxLineMom*100).toFixed(1)}%;background:{COLORS[line]||'#c9a84c'}"><span>{fmtM(total)}</span></div></div></div>
              {/each}
            {/if}
          </div>
        </div>
      {/if}

      {#if momentumView==='year'}
        <div class="charts">
          <div class="chart">
            <h3>Momentum by Build Decade</h3>
            <div class="sub">Total speedDWT1000 grouped by decade</div>
            {#each byDecadeMom as [d, total, n]}
              <div class="br"><div class="bl">{d}s <span style="color:#2a3355;margin-left:4px">({n})</span></div><div class="bt"><div class="bf" style="width:{(total/maxDecMom*100).toFixed(1)}%;background:linear-gradient(90deg,#1a4a6e,#7ec8c8)"><span>{fmtM(total)}</span></div></div></div>
            {/each}
          </div>
          <div class="chart">
            <h3>Momentum by Build Year</h3>
            <div class="sub">Total speedDWT1000 per build year</div>
            {#each byYearMom as [y, total, n]}
              <div class="br"><div class="bl">{y} <span style="color:#2a3355;margin-left:4px">({n})</span></div><div class="bt"><div class="bf" style="width:{(total/maxYearMom*100).toFixed(1)}%;background:linear-gradient(90deg,#1a3a6e,#c9a84c)"><span>{fmtM(total)}</span></div></div></div>
            {/each}
          </div>
        </div>
      {/if}

      {#if momentumView==='range'}
        <div class="rng-box">
          <div class="rng-title">Filter by Build Year Range</div>
          <div class="rng-row">
            <label>From <strong>{yearFrom}</strong> <input type="range" min="1990" max="2019" bind:value={yearFrom}/></label>
            <label>To <strong>{yearTo}</strong>   <input type="range" min="1990" max="2019" bind:value={yearTo}/></label>
          </div>
          <div class="rng-info">{rangeShips.length} ships built {yr0}–{yr1}</div>
        </div>
        <div class="charts">
          <div class="chart wide">
            <h3>Date Range Momentum — {yr0}–{yr1} — {groupBy==='Ship'?'Top 15 Ships':'By Cruise Line'}</h3>
            <div class="sub">speedDWT1000 for ships built within selected range</div>
            {#if groupBy==='Ship'}
              {#each byRangeShip as s}
                <div class="br"><div class="bl" title={s['Ship Name']}><span style={dot(s.CruiseLine,6)}></span>{s['Ship Name']}</div><div class="bt"><div class="bf" style="width:{(s.momentum/maxRangeMom*100).toFixed(1)}%;background:{COLORS[s.CruiseLine]||'#c9a84c'}"><span>{fmtM(s.momentum)}</span></div></div></div>
              {:else}<div style="color:#3a4870;font-size:13px;padding:20px 0">No ships in this range.</div>
              {/each}
            {:else}
              {#each byRangeLine as [line, total]}
                <div class="br"><div class="bl" title={line}><span style={dot(line,6)}></span>{line}</div><div class="bt"><div class="bf" style="width:{(total/maxRangeMom*100).toFixed(1)}%;background:{COLORS[line]||'#c9a84c'}"><span>{fmtM(total)}</span></div></div></div>
              {:else}<div style="color:#3a4870;font-size:13px;padding:20px 0">No ships in this range.</div>
              {/each}
            {/if}
          </div>
        </div>
      {/if}

      {#if momentumView==='voyage'}
        <div class="tw">
          <div class="th"><h3>Voyage Momentum — All Vessels</h3><span>{momBase.length} vessels · ranked by speedDWT1000</span></div>
          <table>
            <thead><tr><th>Ship</th><th>Cruise Line</th><th>Built</th><th>Speed (kts)</th><th>DWT</th><th>speedDWT1000</th></tr></thead>
            <tbody>
              {#each [...momBase].sort((a,b) => b.momentum-a.momentum) as s}
                <tr on:click={()=>selectedShip=s}>
                  <td><span class="sn">{s['Ship Name']}</span>{#if s.IsRiver}<span class="rb">RIVER</span>{/if}</td>
                  <td><span style={dot(s.CruiseLine)}></span>{s.CruiseLine}</td>
                  <td>{s.YearBuilt}</td>
                  <td>{s.Speed} kts</td>
                  <td>{s.DWT.toLocaleString()}</td>
                  <td class="mom-val">{fmtM(s.momentum)}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
      {/if}

      <!-- Fleet table (fleet + analytics tabs) -->
      {#if activeTab !== 'momentum'}
      <div class="tw">
        {#if groupBy==='Ship'}
          <div class="th"><h3>Fleet Registry</h3><span>{filtered.length} vessels</span></div>
          <table>
            <thead><tr>{#each SHIP_COLS as [k,label]}<th on:click={()=>sortBy(k)}>{label}{sortKey===k?(sortDir>0?' ↑':' ↓'):''}</th>{/each}</tr></thead>
            <tbody>
              {#each filtered as s}
                <tr on:click={()=>selectedShip=s}>
                  <td><span class="sn">{s['Ship Name']}</span>{#if s.IsRiver}<span class="rb">RIVER</span>{/if}</td>
                  <td><span style={dot(s.CruiseLine)}></span>{s.CruiseLine}</td>
                  <td>{s.YearBuilt}</td><td>{s.GT.toLocaleString()}</td>
                  <td>{s.PassengerCapacity.toLocaleString()}</td><td>{s.CrewCount.toLocaleString()}</td>
                  <td>{s.DWT.toLocaleString()}</td><td>{s.Speed} kts</td>
                  <td class="mom-val">{fmtM(s.momentum)}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        {:else}
          <div class="th"><h3>Cruise Line Summary</h3><span>{lineGroups.length} lines</span></div>
          <table>
            <thead><tr>{#each LINE_COLS as [k,label]}<th on:click={()=>sortLineBy(k)}>{label}{lineSortKey===k?(lineSortDir>0?' ↑':' ↓'):''}</th>{/each}</tr></thead>
            <tbody>
              {#each lineGroups as l}
                <tr>
                  <td><span style={dot(l.CruiseLine,8)}></span><span class="sn">{l.CruiseLine}</span></td>
                  <td>{l.shipCount}</td><td>{l.totalPax.toLocaleString()}</td>
                  <td>{l.totalCrew.toLocaleString()}</td><td>{l.avgYear}</td>
                  <td>{l.totalDWT.toLocaleString()}</td><td class="mom-val">{fmtM(l.totalMomentum)}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        {/if}
      </div>
      {/if}

    </div>
  </main>
</div>

<!-- Ship detail modal -->
{#if selectedShip}
<div class="ov" on:click|self={()=>selectedShip=null} on:keydown={e=>e.key==='Escape'&&(selectedShip=null)} role="dialog" aria-modal="true">
  <div class="mo">
    <button class="xb" on:click={()=>selectedShip=null}>✕</button>
    <h2>{selectedShip['Ship Name']}</h2>
    <div class="bar"></div>
    <div class="li"><span style={dot(selectedShip.CruiseLine,10)}></span>{selectedShip.CruiseLine}{#if selectedShip.IsRiver}<span class="rb">RIVER</span>{/if}</div>
    <div class="mg">
      <div class="ms"><span class="clbl">Year Built</span>         <div class="mv">{selectedShip.YearBuilt}</div>                              <small>{2019-selectedShip.YearBuilt} years in service</small></div>
      <div class="ms"><span class="clbl">Gross Tonnage</span>      <div class="mv">{selectedShip.GT.toLocaleString()}</div>                   <small>GT — total ship volume</small></div>
      <div class="ms"><span class="clbl">Passengers</span>         <div class="mv">{selectedShip.PassengerCapacity.toLocaleString()}</div>     <small>max capacity</small></div>
      <div class="ms"><span class="clbl">Crew Members</span>       <div class="mv">{selectedShip.CrewCount.toLocaleString()}</div>             <small>{(selectedShip.PassengerCapacity/selectedShip.CrewCount).toFixed(1)} pax per crew</small></div>
      <div class="ms"><span class="clbl">Dead Weight Tonnage</span><div class="mv">{selectedShip.DWT.toLocaleString()}</div>                  <small>DWT — vessel mass</small></div>
      <div class="ms"><span class="clbl">Service Speed</span>      <div class="mv">{selectedShip.Speed} kts</div>                             <small>{selectedShip.IsRiver?'river':'ocean'} service speed</small></div>
      <div class="ms" style="grid-column:1/-1">
        <span class="clbl">Momentum (speedDWT1000)</span>
        <div class="mv teal">{fmtM(selectedShip.momentum)}</div>
        <small>{selectedShip.Speed} kts × {selectedShip.DWT.toLocaleString()} DWT × 1,000</small>
      </div>
    </div>
  </div>
</div>
{/if}
