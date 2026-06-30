export interface User {
  id: string;
  email: string;
  displayName: string;
}

export interface AuthResponse {
  user: User;
}

export interface RegisterPayload {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}
