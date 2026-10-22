export interface ApiConfig {
  readonly baseUrl: string;
}

export const DEFAULT_API_CONFIG: ApiConfig = {
  baseUrl: 'http://localhost:5091',
};
