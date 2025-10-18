import React, { useEffect, useState } from 'react'
import { useMsal, useAccount, useIsAuthenticated } from '@azure/msal-react'
import { loginRequest } from '../msalConfig'
import Chart from 'chart.js/auto'

const API_BASE = 'https://<YOUR_FUNCTION_APP>.azurewebsites.net/api'

export default function Dashboard(){
  const { instance } = useMsal()
  const isAuthenticated = useIsAuthenticated()
  const account = useAccount(instance.getAllAccounts()[0] || {})

  const [records, setRecords] = useState([])
  const [chart, setChart] = useState(null)

  useEffect(()=>{ initChart(); if(isAuthenticated) fetchData(); }, [isAuthenticated])

  const initChart = ()=>{
    const ctx = document.getElementById('dataChart')?.getContext('2d')
    if(!ctx) return
    const c = new Chart(ctx, { type:'bar', data:{ labels:[], datasets:[] }, options:{ responsive:true, maintainAspectRatio:false } })
    setChart(c)
  }

  const updateChart = (data)=>{
    if(!chart) return
    chart.data = {
      labels: data.map(r=>r.id.substring(0,8)),
      datasets: [{ label:'Value A', data:data.map(r=>r.valueA) }, { label:'Value B', data:data.map(r=>r.valueB) }]
    }
    chart.update()
  }

  async function getToken(){
    try{
      const resp = await instance.acquireTokenSilent({ ...loginRequest, account: instance.getAllAccounts()[0] })
      return resp.accessToken
    }catch(e){
      const resp = await instance.acquireTokenPopup(loginRequest)
      return resp.accessToken
    }
  }

  async function fetchData(){
    const token = await getToken()
    const res = await fetch(`${API_BASE}/secure/data`, { headers: { 'Authorization': `Bearer ${token}` } })
    if(res.ok){ const data = await res.json(); setRecords(data); updateChart(data) }
    else console.error('fetch failed', res.status)
  }

  const signIn = ()=> instance.loginPopup(loginRequest)
  const signOut = ()=> instance.logoutPopup()

  const addSample = async ()=>{
    const token = await getToken()
    const payload = { id: crypto.randomUUID(), category:'New Sample', valueA: Math.floor(Math.random()*200), valueB: Math.floor(Math.random()*200) }
    const res = await fetch(`${API_BASE}/secure/data`, { method:'POST', headers:{ 'Content-Type':'application/json', 'Authorization':`Bearer ${token}` }, body: JSON.stringify(payload) })
    if(res.ok) fetchData(); else alert('Create failed: '+res.status)
  }

  return (
    <div className="container">
      <div style={{display:'flex', justifyContent:'space-between', alignItems:'center'}}>
        <h1>Cosmos Dashboard (React)</h1>
        <div>
          {isAuthenticated ? <><button onClick={signOut}>Sign out</button><span style={{marginLeft:10}}>Signed as {account?.username}</span></> : <button onClick={signIn}>Sign in</button>}
        </div>
      </div>

      <p>Authenticated: {isAuthenticated ? 'Yes' : 'No'}</p>

      <div style={{display:'flex', gap:20}}>
        <div style={{flex:2, background:'#fff', padding:12, borderRadius:8}}>
          <h3>Records</h3>
          <table style={{width:'100%'}}>
            <thead><tr><th>ID</th><th>Category</th><th>Value A</th><th>Value B</th></tr></thead>
            <tbody>{records.map(r=> <tr key={r.id}><td>{r.id.substring(0,8)}</td><td>{r.category}</td><td>{r.valueA}</td><td>{r.valueB}</td></tr>)}</tbody>
          </table>
        </div>
        <div style={{flex:1, background:'#fff', padding:12, borderRadius:8}}>
          <h3>Chart</h3>
          <div style={{height:300}}><canvas id="dataChart"></canvas></div>
          <div style={{marginTop:12}}><button onClick={addSample}>Add Sample (admin only)</button></div>
        </div>
      </div>
    </div>
  )
}
