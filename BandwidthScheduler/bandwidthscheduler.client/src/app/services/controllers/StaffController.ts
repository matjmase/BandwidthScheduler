import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ITeam } from '../../models/db/ITeam';
import { Observable } from 'rxjs';
import { IUser } from '../../models/db/IUser';
import { IAllAndTeamUsers } from '../../models/IAllAndTeamUsers';
import { IStaffTeamChangeRequest } from '../../models/IStaffTeamChangeRequest';
import { JsonCustom } from '../../models/JsonCustom';
import { SimplePrimitiveRequest } from '../../models/SimplePrimitiveRequest';

export class StaffController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'staff/';
  }

  public GetAllTeams(): Observable<ITeam[]> {
    return this.http.get<ITeam[]>(this._baseUrl);
  }

  public UpdateTeam(updated: ITeam): Observable<void> {
    return this.http.put<void>(this._baseUrl, updated);
  }

  public DeleteTeam(teamId: number): Observable<void> {
    const headers = new HttpHeaders().set(
      'teamId',
      JsonCustom.stringify(teamId)
    );
    return this.http.delete<void>(this._baseUrl, { headers: headers });
  }

  public GetMyTeams(): Observable<ITeam[]> {
    return this.http.get<ITeam[]>(this._baseUrl + 'myteams');
  }

  public PostTeam(teamName: string): Observable<void> {
    return this.http.post<void>(
      this._baseUrl,
      new SimplePrimitiveRequest(teamName)
    );
  }

  public GetAllAndTeamUsers(teamId: number): Observable<IAllAndTeamUsers> {
    return this.http.get<IAllAndTeamUsers>(
      this._baseUrl + 'alluserandteamuser/' + teamId
    );
  }

  public PostTeamChange(change: IStaffTeamChangeRequest): Observable<void> {
    return this.http.post<void>(this._baseUrl + 'teamchange', change);
  }
}
