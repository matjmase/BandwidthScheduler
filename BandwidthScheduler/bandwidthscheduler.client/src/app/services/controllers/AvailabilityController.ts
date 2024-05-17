import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AvailabilityEntry } from '../../models/db/AvailabilityEntry';
import { IAvailabilityCommitmentResponse } from '../../models/IAvailabilityCommitmentResponse';
import { DateTimeRangeSelectorModel } from '../../commonControls/date-time-range-selector/date-time-range-selector-model';
import { JsonCustom } from '../../models/JsonCustom';

export class AvailabilityController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'availability/';
  }

  public GetAllTimes(
    range: DateTimeRangeSelectorModel
  ): Observable<IAvailabilityCommitmentResponse> {
    const headers = new HttpHeaders()
      .set('start', JsonCustom.stringify(range.start))
      .set('end', JsonCustom.stringify(range.end));
    return this.http.get<IAvailabilityCommitmentResponse>(this._baseUrl, {
      headers: headers,
    });
  }

  public PutAllTimes(
    range: DateTimeRangeSelectorModel,
    times: AvailabilityEntry[]
  ): Observable<void> {
    return this.http.put<void>(this._baseUrl, {
      rangeRequested: range,
      times: times,
    });
  }
}

export class AvailabilityPutRequest {
  constructor(public dayRequested: Date, public times: AvailabilityEntry[]) {}
}
