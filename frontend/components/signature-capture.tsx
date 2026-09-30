'use client';

import { useRef, useState, type PointerEvent } from 'react';

export function SignatureCapture({ onSave, saving }: Readonly<{ onSave: (signatureDataUrl: string) => Promise<void>; saving: boolean }>) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const [acceptedTerms, setAcceptedTerms] = useState(false);

  function point(event: PointerEvent<HTMLCanvasElement>) {
    const canvas = event.currentTarget;
    const bounds = canvas.getBoundingClientRect();
    return {
      x: (event.clientX - bounds.left) * canvas.width / bounds.width,
      y: (event.clientY - bounds.top) * canvas.height / bounds.height,
    };
  }

  function startDrawing(event: PointerEvent<HTMLCanvasElement>) {
    const canvas = event.currentTarget;
    const context = canvas.getContext('2d');
    if (!context) return;
    canvas.setPointerCapture(event.pointerId);
    const position = point(event);
    context.beginPath();
    context.arc(position.x, position.y, 1.5, 0, Math.PI * 2);
    context.fill();
    context.beginPath();
    context.moveTo(position.x, position.y);
    context.lineWidth = 3;
    context.lineCap = 'round';
    context.lineJoin = 'round';
    context.strokeStyle = '#17493f';
    setIsDrawing(true);
  }

  function draw(event: PointerEvent<HTMLCanvasElement>) {
    if (!isDrawing) return;
    const context = event.currentTarget.getContext('2d');
    if (!context) return;
    const position = point(event);
    context.lineTo(position.x, position.y);
    context.stroke();
  }

  function clear() {
    const canvas = canvasRef.current;
    const context = canvas?.getContext('2d');
    if (!canvas || !context) return;
    context.clearRect(0, 0, canvas.width, canvas.height);
    setIsDrawing(false);
  }

  async function submit() {
    const canvas = canvasRef.current;
    if (!canvas || !acceptedTerms) return;
    const context = canvas.getContext('2d');
    const pixels = context?.getImageData(0, 0, canvas.width, canvas.height).data;
    if (!pixels || !pixels.some((channel, index) => index % 4 === 3 && channel > 0)) return;
    await onSave(canvas.toDataURL('image/png'));
  }

  return <section className="panel compact-form acceptance-panel">
    <h2>Registrar aceite</h2>
    <p>Assine no quadro e confirme que revisou o relatório da vistoria. O registro identifica sua conta, data e imagem da assinatura.</p>
    <canvas ref={canvasRef} className="signature-canvas" width={720} height={220} aria-label="Área para desenhar a assinatura"
      onPointerDown={startDrawing} onPointerMove={draw} onPointerUp={() => setIsDrawing(false)} onPointerCancel={() => setIsDrawing(false)} />
    <button className="button button--outline" type="button" onClick={clear} disabled={saving}>Limpar assinatura</button>
    <label className="acceptance-consent"><input type="checkbox" checked={acceptedTerms} onChange={(event) => setAcceptedTerms(event.target.checked)} disabled={saving} /><span>Confirmo que revisei e concordo com as informações do relatório desta vistoria.</span></label>
    <p className="panel-caption">Este é um registro eletrônico de aceite; não é uma assinatura digital certificada.</p>
    <button className="button button--primary" type="button" disabled={saving || !acceptedTerms} onClick={submit}>{saving ? 'Registrando...' : 'Confirmar aceite'}</button>
  </section>;
}
