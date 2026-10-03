import { useCallback, useEffect, useState } from 'react'
import { api, type Department } from '../api'
import { ErrorBox } from '../components/Bits'

export default function Departments() {
  const [list, setList] = useState<Department[]>([])
  const [error, setError] = useState<string | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [name, setName] = useState('')
  const [desc, setDesc] = useState('')
  const load = useCallback(() => api.departments().then(setList).catch(e => setError(e.message)), [])
  useEffect(() => { load() }, [load])

  const reset = () => { setEditId(null); setName(''); setDesc('') }
  async function save(e: React.FormEvent) {
    e.preventDefault(); setError(null)
    try { await api.saveDepartment(editId, { name, description: desc || undefined }); reset(); load() }
    catch (err) { setError((err as Error).message) }
  }
  async function del(d: Department) {
    setError(null)
    if (!confirm(`Delete department "${d.name}"?`)) return
    try { await api.deleteDepartment(d.id); load() } catch (err) { setError((err as Error).message) }
  }

  return (
    <>
      <h1>Departments</h1>
      <ErrorBox error={error} />
      <form onSubmit={save} className="inline-form panel">
        <label>Name<input required value={name} onChange={e => setName(e.target.value)} /></label>
        <label className="grow">Description<input value={desc} onChange={e => setDesc(e.target.value)} /></label>
        <button className="btn primary">{editId ? 'Update' : 'Add'}</button>
        {editId && <button type="button" className="btn" onClick={reset}>Cancel</button>}
      </form>
      <table>
        <thead><tr><th>Name</th><th>Description</th><th className="num">KPIs</th><th /></tr></thead>
        <tbody>
          {list.map(d => (
            <tr key={d.id}>
              <td>{d.name}</td><td className="muted">{d.description}</td><td className="num">{d.kpiCount}</td>
              <td className="num">
                <button className="link" onClick={() => { setEditId(d.id); setName(d.name); setDesc(d.description ?? '') }}>Edit</button>{' '}
                <button className="link danger" onClick={() => del(d)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  )
}
