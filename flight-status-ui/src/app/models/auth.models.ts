export interface LoginRequest   { email: string; password: string; }
export interface RegisterRequest { firstName: string; lastName: string; email: string; password: string; }
export interface LoginResponse  {
  token: string; email: string; firstName: string; lastName: string;
  role: string; expiresAt: string;
}
export interface CurrentUser { email: string; firstName: string; lastName: string; role: string; }
