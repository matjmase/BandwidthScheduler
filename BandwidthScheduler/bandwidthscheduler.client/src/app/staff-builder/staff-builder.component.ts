import { Component, OnInit, ViewChild } from '@angular/core';
import { FormControl, NgForm } from '@angular/forms';
import { Observable, map, startWith } from 'rxjs';
import { BackendConnectService } from '../services/backend-connect.service';
import { ITeam } from '../models/ITeam';
import { MatSnackBar } from '@angular/material/snack-bar';
import { IAllAndTeamUsers } from '../models/IAllAndTeamUsers';
import { TeamSelectorComponent } from '../commonControls/team-selector/team-selector.component';

@Component({
  selector: 'app-staff-builder',
  templateUrl: './staff-builder.component.html',
  styleUrl: './staff-builder.component.scss',
})
export class StaffBuilderComponent {
  @ViewChild('teamSelector')
  teamSelector!: TeamSelectorComponent;

  options: { [key: string]: ITeam } = {};
  dbTeamSelected: ITeam | undefined;

  public TeamAdded(): void {
    this.teamSelector.GetTeamsAutoComplete();
  }

  public TeamSelected(team: ITeam): void {
    this.dbTeamSelected = team;
  }
}
