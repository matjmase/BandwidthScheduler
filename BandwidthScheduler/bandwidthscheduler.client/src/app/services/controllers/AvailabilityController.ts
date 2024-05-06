import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AvailabilityEntry } from '../../models/AvailabilityEntry';
import { IAvailabilityCommitmentResponse } from '../../models/IAvailabilityCommitmentResponse';

export class AvailabilityController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'availability/';
  }

  public GetAllTimes(date: Date): Observable<IAvailabilityCommitmentResponse> {
    return this.http.get<IAvailabilityCommitmentResponse>(this._baseUrl, {
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
