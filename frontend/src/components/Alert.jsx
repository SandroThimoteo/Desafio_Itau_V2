export default function Alert({ msg, type }) {
  if (!msg) return null;
  return <div className={`alert ${type}`}>{msg}</div>;
}
