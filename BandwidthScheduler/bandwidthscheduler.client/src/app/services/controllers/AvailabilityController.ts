import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { IAvailabilityResponse } from '../../models/IAvailabilityResponse';
import { AvailabilityEntry } from '../../models/IAvailabilityEntry';

export class AvailabilityController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'availability/';
  }

  public GetAllTimes(date: Date): Observable<IAvailabilityResponse[]> {
    return this.http.get<IAvailabilityResponse[]>(this._baseUrl, {
      headers: {
        dayRequested: date.toUTCString(),
      },
    });
  }

  public PutAllTimes(date: Date, times: AvailabilityEntry[]): Observable<void> {
    return this.http.put<void>(this._baseUrl, {
      dayRequested: date,
      times: times,
    });
  }
}

export class AvailabilityPutRequest {
  constructor(public dayRequested: Date, public times: AvailabilityEntry[]) {}
}
