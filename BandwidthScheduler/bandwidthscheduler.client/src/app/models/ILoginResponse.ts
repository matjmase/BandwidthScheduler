export interface ILoginResponse {
  id: number;
  email: string;
  roles: string[];
  token: string;
  validUntil: string;
}
