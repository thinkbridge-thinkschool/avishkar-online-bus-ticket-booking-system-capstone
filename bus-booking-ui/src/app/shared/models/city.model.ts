export interface City {
  cityId: string;
  cityName: string;
  stateCode?: string;
}

export interface CreateCityRequest {
  cityName: string;
}
