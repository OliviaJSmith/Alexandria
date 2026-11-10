import { config } from '../config';

/** An API failure with a message that is safe to show the user. `status` is 0 when the API could not be reached. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export type ApiClient = {
  get<T>(path: string): Promise<T>;
  post<T>(path: string, body?: unknown): Promise<T>;
  put<T>(path: string, body: unknown): Promise<T>;
  patch<T>(path: string, body: unknown): Promise<T>;
  delete(path: string, body?: unknown): Promise<void>;
  /** multipart/form-data upload; the browser/runtime sets the boundary header. */
  postForm<T>(path: string, form: FormData): Promise<T>;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

/**
 * Fetch wrapper for the Alexandria API. Adds the bearer token, turns ProblemDetails into readable
 * errors, and calls `onUnauthorized` when an authenticated request is rejected (expired or revoked token).
 */
export function createApiClient(getToken: () => string | null, onUnauthorized: () => void): ApiClient {
  async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const token = getToken();
    const isForm = body instanceof FormData;
    const headers: Record<string, string> = { Accept: 'application/json' };
    if (body !== undefined && !isForm) {
      headers['Content-Type'] = 'application/json';
    }
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    let response: Response;
    try {
      response = await fetch(`${config.apiUrl}${path}`, {
        method,
        headers,
        body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
      });
    } catch {
      throw new ApiError(0, 'Could not reach Alexandria. Check your connection and try again.');
    }

    if (response.status === 401 && token) {
      onUnauthorized();
      throw new ApiError(401, 'Your session has expired. Please sign in again.');
    }

    if (!response.ok) {
      throw new ApiError(response.status, await readErrorMessage(response));
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }

  return {
    get: (path) => request('GET', path),
    post: (path, body) => request('POST', path, body),
    put: (path, body) => request('PUT', path, body),
    patch: (path, body) => request('PATCH', path, body),
    delete: (path, body) => request('DELETE', path, body),
    postForm: (path, form) => request('POST', path, form),
  };
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    if (problem.detail) {
      return problem.detail;
    }
    const firstValidationError = problem.errors && Object.values(problem.errors).flat()[0];
    if (firstValidationError) {
      return firstValidationError;
    }
    if (problem.title) {
      return problem.title;
    }
  } catch {
    // Not JSON; fall through to a generic message.
  }
  return `Something went wrong (${response.status}). Please try again.`;
}

export function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
