const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL || '').replace(/\/+$/, '');

interface ApiErrorResponse {
  code?: string;
  error?: string;
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.method && !['GET', 'HEAD', 'OPTIONS'].includes(init.method.toUpperCase())) {
    headers.set('X-Vistora-Request', '1');
  }
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers,
      credentials: 'include',
    });
  } catch {
    throw new Error('Não foi possível conectar à API. Confira se o Docker está em execução.');
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null) as ApiErrorResponse | null;
    const validationMessage = body?.errors
      ? Object.values(body.errors).flat()[0]
      : undefined;
    const message = response.status === 401
      ? path.endsWith('/auth/login') ? 'E-mail ou senha inválidos.' : 'Sua sessão expirou. Entre novamente.'
      : body?.code === 'email_in_use'
        ? 'Já existe uma conta com esse e-mail.'
        : response.status === 429
          ? 'Muitas tentativas. Aguarde um minuto e tente novamente.'
          : validationMessage || body?.error || body?.detail || body?.title || 'Não foi possível concluir a solicitação.';
    throw new Error(message);
  }

  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}

export async function apiBlob(path: string): Promise<Blob> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, { credentials: 'include' });
  } catch {
    throw new Error('Não foi possível conectar à API. Confira se o Docker está em execução.');
  }
  if (!response.ok) throw new Error('Não foi possível carregar o arquivo privado.');
  return response.blob();
}
