import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ITeam } from '../../models/db/ITeam';
import { Observable } from 'rxjs';
import { IUser } from '../../models/db/IUser';
import { IAllAndTeamUsers } from '../../models/IAllAndTeamUsers';
import { IStaffTeamChangeRequest } from '../../models/IStaffTeamChangeRequest';

export class StaffController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'staff/';
  }

  public GetAllTeams(): Observable<ITeam[]> {
    return this.http.get<ITeam[]>(this._baseUrl);
  }

  public GetMyTeams(): Observable<ITeam[]> {
    return this.http.get<ITeam[]>(this._baseUrl + 'myteams');
  }

  public PostTeam(teamName: string): Observable<void> {
    return this.http.post<void>(this._baseUrl, { text: teamName });
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
